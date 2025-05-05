using System.Drawing;
using System.Drawing.Imaging;
using QRCoder;

namespace eventease_app.Controllers
{
    internal class QRCode
    {
        private QRCodeData qrCodeData;

        public QRCode(QRCodeData qrCodeData)
        {
            this.qrCodeData = qrCodeData;
        }

        internal Bitmap GetGraphic(int pixelsPerModule)
        {
            // Create an instance of QRCoder's QRCode class without using a 'using' statement
            QRCoder.QRCode qrCode = new(qrCodeData);
            try
            {
                return qrCode.GetGraphic(pixelsPerModule);
            }
            finally
            {
                // Explicitly dispose of the QRCode instance to avoid CS1674
                qrCode.Dispose();
            }
        }
    }
}