using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using System.Collections.Concurrent;
using System.Drawing.Imaging;
using TPU_Assembly_Inspection_Paddle;

namespace TPU_Assembly.Class
{
    public class YoloInferenceEngine : IDisposable
    {
        private readonly InferenceSession _session;
        private readonly string _inputName;
        private readonly int _inputHeight;
        private readonly int _inputWidth;

        private int _numClasses;
        private int _numPredictions;
        public float NmsThreshold { get; set; } = 0.45f;

        public YoloInferenceEngine(string modelPath)
        {
            var options = new SessionOptions();

            try
            {
                options.AppendExecutionProvider_OpenVINO("CPU");

            }
            catch (Exception ex)
            {
                Console.WriteLine("OpenVINO not supported, using default CPU: " + ex.Message);
            }

            _session = new InferenceSession(modelPath, options);

            var inputMeta = _session.InputMetadata.First();
            _inputName = inputMeta.Key;
            _inputHeight = inputMeta.Value.Dimensions[2];
            _inputWidth = inputMeta.Value.Dimensions[3];

            var outputMeta = _session.OutputMetadata.First();
            var shape = outputMeta.Value.Dimensions;

            if (shape[2] > shape[1])
            {

                _numPredictions = shape[2];
                int channels = shape[1];
                _numClasses = channels - 4;
            }
            else
            {
                _numPredictions = shape[1];
                int channels = shape[2];
                _numClasses = channels - 4;
            }
        }

        public List<Detection> RunInference(Bitmap image)
        {
            Tensor<float> inputTensor = PreprocessImage(image);

            var inputs = new List<NamedOnnxValue> { NamedOnnxValue.CreateFromTensor(_inputName, inputTensor) };

            using (var results = _session.Run(inputs))
            {
                var outputTensor = results.First().AsTensor<float>();

                return ParseOutputOptimized(outputTensor, image.Width, image.Height);
            }
        }
        public Dictionary<string, float> GetTotalAreaPerClass(List<Detection> detections)
        {
            return detections
                .GroupBy(d => d.ClassName)
                .ToDictionary(
                    group => group.Key,
                    group => group.Sum(d => d.Area)
                );
        }

        private Tensor<float> PreprocessImage(Bitmap image)
        {
            var tensor = new DenseTensor<float>(new[] { 1, 3, _inputHeight, _inputWidth });

            using (Bitmap resized = new Bitmap(image, new Size(_inputWidth, _inputHeight)))
            {
                BitmapData data = resized.LockBits(new Rectangle(0, 0, _inputWidth, _inputHeight), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);

                unsafe
                {
                    byte* ptr = (byte*)data.Scan0;
                    int stride = data.Stride;
                    int h = _inputHeight;
                    int w = _inputWidth;

                    Parallel.For(0, h, y =>
                    {
                        byte* row = ptr + (y * stride);
                        for (int x = 0; x < w; x++)
                        {
                            int b = row[x * 3 + 0];
                            int g = row[x * 3 + 1];
                            int r = row[x * 3 + 2];

                            tensor[0, 0, y, x] = r / 255.0f;
                            tensor[0, 1, y, x] = g / 255.0f;
                            tensor[0, 2, y, x] = b / 255.0f;
                        }
                    });
                }
                resized.UnlockBits(data);
            }
            return tensor;
        }

        private List<Detection> ParseOutputOptimized(Tensor<float> output, int orgWidth, int orgHeight)
        {
            var detections = new ConcurrentBag<Detection>();
            float[] outputData = output.ToArray(); 
            float xRatio = (float)orgWidth / _inputWidth;
            float yRatio = (float)orgHeight / _inputHeight;


            Parallel.For(0, _numPredictions, i =>
            {
                float maxConf = 0;
                int maxClassId = -1;

                for (int c = 0; c < _numClasses; c++)
                {
                    int classRow = 4 + c;
                    float score = outputData[classRow * _numPredictions + i];

                    if (score > maxConf)
                    {
                        maxConf = score;
                        maxClassId = c;
                    }
                }

                if (maxConf >= MAINFORM.ConfidenceThreshold)
                {
                    float x = outputData[0 * _numPredictions + i];
                    float y = outputData[1 * _numPredictions + i];
                    float w = outputData[2 * _numPredictions + i];
                    float h = outputData[3 * _numPredictions + i];

                    float xMin = (x - w / 2) * xRatio;
                    float yMin = (y - h / 2) * yRatio;
                    float width = w * xRatio;
                    float height = h * yRatio;

                    detections.Add(new Detection
                    {
                        ClassId = maxClassId,
                        Confidence = maxConf,
                        X = xMin,
                        Y = yMin,
                        Width = width,
                        Height = height,
                        ClassName = maxClassId.ToString()
                    });
                }
            });

            return ApplyNmsFast(detections.ToList(), NmsThreshold);
        }

        private List<Detection> ApplyNmsFast(List<Detection> detections, float threshold)
        {
            if (detections.Count == 0) return new List<Detection>();

            detections.Sort((a, b) => b.Confidence.CompareTo(a.Confidence));

            List<Detection> result = new List<Detection>();
            bool[] isActive = new bool[detections.Count];
            for (int i = 0; i < isActive.Length; i++) isActive[i] = true;

            for (int i = 0; i < detections.Count; i++)
            {
                if (!isActive[i]) continue;

                var best = detections[i];
                result.Add(best);

                for (int j = i + 1; j < detections.Count; j++)
                {
                    if (isActive[j])
                    {
                        if (IoU(best, detections[j]) > threshold)
                        {
                            isActive[j] = false;
                        }
                    }
                }
            }
            return result;
        }

        private float IoU(Detection a, Detection b)
        {
            float x1 = Math.Max(a.X, b.X);
            float y1 = Math.Max(a.Y, b.Y);
            float x2 = Math.Min(a.X + a.Width, b.X + b.Width);
            float y2 = Math.Min(a.Y + a.Height, b.Y + b.Height);

            float w = Math.Max(0, x2 - x1);
            float h = Math.Max(0, y2 - y1);
            float inter = w * h;

            float areaA = a.Width * a.Height;
            float areaB = b.Width * b.Height;

            return inter / (areaA + areaB - inter + 1e-6f);
        }

        public void Dispose()
        {
            _session?.Dispose();
        }

        public class Detection
        {
            public int ClassId { get; set; }
            public string ClassName { get; set; }
            public float Confidence { get; set; }
            public float X { get; set; }
            public float Y { get; set; }
            public float Width { get; set; }
            public float Height { get; set; }

            public float Area => Width * Height;

            public RectangleF Rect => new (X, Y, Width, Height);

            public override string ToString()
            {
                return $"Class: {ClassName} | Conf: {Confidence:0.00} | Area: {Area:0.00}";
            }
        }
    }
}