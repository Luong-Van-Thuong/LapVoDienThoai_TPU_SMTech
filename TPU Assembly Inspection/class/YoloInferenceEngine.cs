using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace TPU_Assembly.Class
{
    public class YoloInferenceEngine : IDisposable
    {
        private readonly InferenceSession _session;
        private readonly string _inputName;
        private readonly int _inputHeight;
        private readonly int _inputWidth;

        // Metadata output
        private int _numClasses;
        private int _numPredictions; // Ví dụ 8400

        // Cấu hình
        public float ConfidenceThreshold { get; set; } = 0.50f;
        public float NmsThreshold { get; set; } = 0.45f;

        public YoloInferenceEngine(string modelPath)
        {
            var options = new SessionOptions();
            // Nếu có GPU, bỏ comment dòng dưới (cần cài package Microsoft.ML.OnnxRuntime.Gpu)
            // options.AppendExecutionProvider_CUDA(0); 

            _session = new InferenceSession(modelPath, options);

            // 1. Lấy thông tin Input
            var inputMeta = _session.InputMetadata.First();
            _inputName = inputMeta.Key;
            _inputHeight = inputMeta.Value.Dimensions[2]; // [1, 3, 640, 640]
            _inputWidth = inputMeta.Value.Dimensions[3];

            // 2. Lấy thông tin Output và tự động detect cấu trúc
            var outputMeta = _session.OutputMetadata.First();
            var shape = outputMeta.Value.Dimensions; // [1, 84, 8400] hoặc [1, 8400, 84]

            // Logic tự động nhận diện Shape (Transpose hay không)
            if (shape[2] > shape[1])
            {
                // Chuẩn: [1, Channels, Anchors] -> [1, 84, 8400]
                _numPredictions = shape[2];
                int channels = shape[1];
                _numClasses = channels - 4; // Trừ 4 toạ độ box
            }
            else
            {
                // Transposed: [1, Anchors, Channels] -> [1, 8400, 84]
                _numPredictions = shape[1];
                int channels = shape[2];
                _numClasses = channels - 4;
            }
        }

        public List<Detection> RunInference(Bitmap image)
        {
            // 1. Preprocessing (Resize & Normalize) - Dùng Parallel để nhanh hơn
            Tensor<float> inputTensor = PreprocessImage(image);

            // 2. Inference
            var inputs = new List<NamedOnnxValue> { NamedOnnxValue.CreateFromTensor(_inputName, inputTensor) };

            using (var results = _session.Run(inputs))
            {
                var outputTensor = results.First().AsTensor<float>();

                // 3. Post-processing (Parse & NMS) - KHÔNG DÙNG LINQ CHẬM CHẠP
                return ParseOutputOptimized(outputTensor, image.Width, image.Height);
            }
        }

        private Tensor<float> PreprocessImage(Bitmap image)
        {
            var tensor = new DenseTensor<float>(new[] { 1, 3, _inputHeight, _inputWidth });

            // Resize ảnh về kích thước model (letterbox giữ tỷ lệ nếu cần, ở đây resize thẳng cho đơn giản code)
            // Khuyên dùng: Nên clone ra ảnh mới resize để đảm bảo thread safety
            using (Bitmap resized = new Bitmap(image, new Size(_inputWidth, _inputHeight)))
            {
                BitmapData data = resized.LockBits(new Rectangle(0, 0, _inputWidth, _inputHeight), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);

                unsafe
                {
                    byte* ptr = (byte*)data.Scan0;
                    int stride = data.Stride;
                    int h = _inputHeight;
                    int w = _inputWidth;

                    // Dùng Parallel For để tận dụng đa nhân CPU khi convert pixel
                    Parallel.For(0, h, y =>
                    {
                        byte* row = ptr + (y * stride);
                        for (int x = 0; x < w; x++)
                        {
                            // Offset pixel 24bpp: B-G-R
                            int b = row[x * 3 + 0];
                            int g = row[x * 3 + 1];
                            int r = row[x * 3 + 2];

                            // Normalize 0..1
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

                if (maxConf >= ConfidenceThreshold)
                {
                    // Lấy box
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

            return inter / (areaA + areaB - inter + 1e-6f); // +1e-6f để tránh chia 0
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

            public RectangleF Rect => new RectangleF(X, Y, Width, Height);

            public override string ToString()
            {
                return $"Class: {ClassName} | Conf: {Confidence:0.00}";
            }
        }
    }
}