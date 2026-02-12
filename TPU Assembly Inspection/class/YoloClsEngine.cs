using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace TPU_Assembly.Class
{
    public class YoloClsPrediction
    {
        public string Label { get; set; }
        public float Confidence { get; set; }
        public int ClassId { get; set; }
    }

    public class YoloClsEngine : IDisposable
    {
        private readonly InferenceSession _session;
        private readonly string[] _labels;
        private readonly int _imgSize;

        // Tên input node trong ONNX (thường là "images")
        private string _inputName;

        public YoloClsEngine(string modelPath, string[] labels, int imgSize = 224, bool useGpu = false)
        {
            _labels = labels;
            _imgSize = imgSize;

            SessionOptions options = new SessionOptions();
            if (useGpu)
            {
                try
                {
                    // Yêu cầu đã cài CUDA và cuDNN tương thích
                    options.AppendExecutionProvider_CUDA(0);
                }
                catch (Exception)
                {
                    Console.WriteLine("Failed to load GPU, falling back to CPU.");
                }
            }

            _session = new InferenceSession(modelPath, options);
            _inputName = _session.InputMetadata.Keys.First();
        }

        public YoloClsPrediction Predict(Bitmap image)
        {
            // 1. Resize ảnh về đúng kích thước train (224x224)
            using (Bitmap resized = ResizeImage(image, _imgSize, _imgSize))
            {
                // 2. Chuyển đổi Bitmap thành Tensor (Normalization 0-1)
                var inputTensor = BitmapToTensor(resized);

                // 3. Tạo input cho ONNX
                var inputs = new List<NamedOnnxValue>
                {
                    NamedOnnxValue.CreateFromTensor(_inputName, inputTensor)
                };

                // 4. Chạy Inference
                using (var results = _session.Run(inputs))
                {
                    // Lấy kết quả đầu ra (Tensor float)
                    // Output của YOLO Cls thường là mảng float [1, num_classes]
                    var output = results.First().AsTensor<float>();
                    float[] scores = output.ToArray();

                    // 5. Tìm class có điểm số cao nhất (Softmax logic đơn giản)
                    // Lưu ý: Output model YOLO đôi khi chưa qua Softmax, nhưng giá trị lớn nhất vẫn là class đúng.
                    return GetBestMatch(scores);
                }
            }
        }

        private DenseTensor<float> BitmapToTensor(Bitmap image)
        {
            // Tạo Tensor kích thước: [1, 3, Height, Width]
            var tensor = new DenseTensor<float>(new[] { 1, 3, image.Height, image.Width });

            BitmapData data = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);

            unsafe
            {
                byte* ptr = (byte*)data.Scan0;
                int stride = data.Stride;

                for (int y = 0; y < image.Height; y++)
                {
                    for (int x = 0; x < image.Width; x++)
                    {
                        // Bitmap trong Windows là BGR, ONNX thường cần RGB
                        // PixelFormat.Format24bppRgb lưu theo thứ tự: Blue, Green, Red

                        byte b = ptr[y * stride + x * 3 + 0];
                        byte g = ptr[y * stride + x * 3 + 1];
                        byte r = ptr[y * stride + x * 3 + 2];

                        // Normalize về khoảng [0, 1] (chia cho 255.0f)
                        tensor[0, 0, y, x] = r / 255.0f;
                        tensor[0, 1, y, x] = g / 255.0f;
                        tensor[0, 2, y, x] = b / 255.0f;
                    }
                }
            }

            image.UnlockBits(data);
            return tensor;
        }

        private YoloClsPrediction GetBestMatch(float[] scores)
        {
            int maxIndex = 0;
            float maxScore = -float.MaxValue;

            for (int i = 0; i < scores.Length; i++)
            {
                if (scores[i] > maxScore)
                {
                    maxScore = scores[i];
                    maxIndex = i;
                }
            }

            // Nếu model trả về logit chưa qua softmax, ta có thể để nguyên hoặc tính softmax nếu cần hiển thị %
            // Ở đây trả về raw confidence hoặc bạn có thể cài thêm hàm Softmax.

            string labelName = (maxIndex < _labels.Length) ? _labels[maxIndex] : "Unknown";

            return new YoloClsPrediction
            {
                ClassId = maxIndex,
                Confidence = maxScore,
                Label = labelName
            };
        }

        private Bitmap ResizeImage(Bitmap image, int width, int height)
        {
            Bitmap resizedImage = new Bitmap(width, height);
            using (Graphics g = Graphics.FromImage(resizedImage))
            {
                g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                g.DrawImage(image, 0, 0, width, height);
            }
            return resizedImage;
        }

        public void Dispose()
        {
            _session?.Dispose();
        }
    }
}