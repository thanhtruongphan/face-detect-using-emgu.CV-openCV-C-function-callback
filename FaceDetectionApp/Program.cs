

using System;
using System.Drawing;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.Dnn;
using Emgu.CV.Util;

class Program
{
    static void Main(string[] args)
    {
        // Đường dẫn tới mô hình
        string modelPath = "models/res10_300x300_ssd_iter_140000.caffemodel";
        string configPath = "models/deploy.prototxt";

        // Tạo mạng DNN
        Net faceNet = DnnInvoke.ReadNetFromCaffe(configPath, modelPath);
        if (faceNet.Empty)
        {
            Console.WriteLine("Failed to load model!");
            return;
        }

        // Khởi tạo video capture từ camera (hoặc dùng file video nếu muốn)
        using (var capture = new VideoCapture(0)) // Thay 0 bằng đường dẫn video nếu cần
        {
            if (!capture.IsOpened)
            {
                Console.WriteLine("Không thể mở camera!");
                return;
            }

            Mat frame = new Mat();
            Console.WriteLine("Nhấn 'ESC' để thoát.");

            while (true)
            {
                // Đọc từng khung hình từ camera
                capture.Read(frame);
                if (frame.IsEmpty) break;

                // Phát hiện khuôn mặt
                Rectangle[] faces = DetectFaces(faceNet, frame);

                // Vẽ hình chữ nhật quanh khuôn mặt
                foreach (var face in faces)
                {
                    CvInvoke.Rectangle(frame, face, new MCvScalar(0, 255, 0), 2);
                }

                // Hiển thị khung hình
                CvInvoke.Imshow("Real-Time Face Detection", frame);

                // Kiểm tra phím bấm
                if (CvInvoke.WaitKey(1) == 27) // ESC để thoát
                    break;
            }
        }

        // Dọn dẹp
        CvInvoke.DestroyAllWindows();
    }

    private static Rectangle[] DetectFaces(Net faceNet, Mat inputImage)
    {
        var detections = new List<Rectangle>();
        int imageWidth = inputImage.Width;
        int imageHeight = inputImage.Height;

        // Tạo blob từ ảnh
        var inputBlob = DnnInvoke.BlobFromImage(inputImage, 1.0, new Size(300, 300), new MCvScalar(104, 177, 123), false, false);

        // Dự đoán
        faceNet.SetInput(inputBlob);
        var output = faceNet.Forward();

        // Xử lý kết quả
        var outputData = output.GetData();
        float[,,,] data = (float[,,,])outputData;

        for (int i = 0; i < data.GetLength(2); i++)
        {
            float confidence = data[0, 0, i, 2];
            if (confidence > 0.5) // Ngưỡng confidence
            {
                int x1 = (int)(data[0, 0, i, 3] * imageWidth);
                int y1 = (int)(data[0, 0, i, 4] * imageHeight);
                int x2 = (int)(data[0, 0, i, 5] * imageWidth);
                int y2 = (int)(data[0, 0, i, 6] * imageHeight);

                detections.Add(new Rectangle(x1, y1, x2 - x1, y2 - y1));
            }
        }

        return detections.ToArray();
    }
}
