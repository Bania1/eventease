
using System.Drawing;

namespace QRCoder
{
    internal class QRCode
    {
        private QRCodeData qrCodeData;

        public QRCode(QRCodeData qrCodeData)
        {
            this.qrCodeData = qrCodeData;
        }

        internal void Dispose()
        {
            if (qrCodeData != null)
            {
                qrCodeData.Dispose(); // Only if QRCodeData implements IDisposable
                qrCodeData = null;
            }
        }

        internal Bitmap GetGraphic(int pixelsPerModule)
        {
            throw new NotImplementedException();
        }
    }
}