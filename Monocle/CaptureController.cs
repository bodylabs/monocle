using Smithers.Reading.FrameData;
using Smithers.Sessions;
using Smithers.Visualization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monocle
{
    public class CaptureController
    {
        FrameReader _reader = new FrameReader();
        BitmapBuilder _bb = new BitmapBuilder();

        string _baseDirectory;
        Session<object, Shot<ShotDefinition, SavedItem>, ShotDefinition, SavedItem> _session;
        
        SessionManager<Session<object, Shot<ShotDefinition, SavedItem>, ShotDefinition, SavedItem>, object, Shot<ShotDefinition, SavedItem>, ShotDefinition, SavedItem> _sessionManager;

        public FrameReader FrameReader { get { return _reader; } }
        public SessionManager<Session<object, Shot<ShotDefinition, SavedItem>, ShotDefinition, SavedItem>, object, Shot<ShotDefinition, SavedItem>, ShotDefinition, SavedItem> SessionManager { get { return _sessionManager; } }
        public Session<object, Shot<ShotDefinition, SavedItem>, ShotDefinition, SavedItem> Session { get { return _session; } }

        public CaptureController(string baseDirectory)
        {
            _baseDirectory = baseDirectory;

            StartNewSession();

            Console.WriteLine("New session in path: " + _session.SessionPath);

            _sessionManager = new SessionManager<Smithers.Sessions.Session<object, Shot<ShotDefinition, SavedItem>, ShotDefinition, SavedItem>, object, Shot<ShotDefinition, SavedItem>, ShotDefinition, SavedItem>(_session);

            _sessionManager.AttachToReader(_reader);
        }

        public void StartCapture()
        {
            // Instead of using a predefined capture program, we add shots on the fly
            _session.AddShot(ShotDefinition.DEFAULT);
            
            _sessionManager.PrepareForNextShot();

            _sessionManager.CaptureShot();
        }

        public void StartNewSession()
        {
            string guid = Guid.NewGuid().ToString();
            string path = Path.Combine(_baseDirectory, guid);

            _session = new Session<object, Shot<ShotDefinition, SavedItem>, ShotDefinition, SavedItem>(path, Enumerable.Empty<ShotDefinition>());
        }

        public ProjectionMode ProjectionMode { get; set; }

        public SkeletonPresenter SkeletonPresenter { get; set; }

    }
}
