using Smithers.Sessions;
using Smithers.Sessions.Archiving;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monocle
{
    public class ApplicationController
    {
        CaptureController _captureController;

        public ApplicationController()
        {
            string baseDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Body Labs", "Monocle");

            _captureController = new CaptureController(baseDirectory);
        }

        public CaptureController CaptureController { get { return _captureController; } }

        public async Task<ArchiveResult> CompressAndStartNewSession()
        {
            Session<object, Shot<ShotDefinition, SavedItem>, ShotDefinition, SavedItem> oldSession = _captureController.Session;

            _captureController.StartNewSession();

            Archiver archiver = new Archiver();
            return await archiver.PerformArchive(oldSession.SessionPath, oldSession.SessionPath + ".scan");
        }
    }
}
