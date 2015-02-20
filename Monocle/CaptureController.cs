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

            _sessionManager = new SessionManager<
                                    Smithers.Sessions.Session<object, 
                                                              Shot<ShotDefinition, SavedItem>, 
                                                              ShotDefinition, 
                                                              SavedItem>, 
                                    object, 
                                    Shot<ShotDefinition, SavedItem>, 
                                    ShotDefinition, 
                                    SavedItem>(_session);

            _sessionManager.AttachToReader(_reader);
        }

        public void StartCapture()
        {
            // Instead of using a predefined capture program, we add shots on the fly
            _session.AddShot(ShotDefinition.DEFAULT);
            
            _sessionManager.PrepareForNextShot();

            _sessionManager.CaptureShot();
        }

        /// <summary>
        /// This function starts a new capture with the specified number of buffers available.
        /// A new Shot (with the specified shot definition) will be added to the Sessions List of Shots 
        /// </summary>
        /// <param name="nMemoryFrames">How many buffers should be used for caching the incoming frames before they are serialized</param>
        /// <param name="nFramesToCapture">Optional parameter: How many frames are supposed to be captured. If not set, the default is 0
        /// and the capture continues until the buffer is full or until the user presses the stop button</param>
        /// <param name="serializationFlags">Struct containing information about which data to save to disk</param>
        public void StartCapture(SerializationFlags serializationFlags, int nMemoryFrames, int nFramesToCapture = 0)
        {
            ShotDefinitionVariableFrames newShot = new ShotDefinitionVariableFrames(nFramesToCapture, nMemoryFrames, serializationFlags);
            _session.AddShot(newShot);
            _sessionManager.PrepareForNextShot();
            _sessionManager.CaptureShot();
        }


        public void StopCapture()
        {
            _sessionManager.StopCapture();
        }

        public void SetBufferSize(Int64 size)
        {
          _sessionManager.SetBufferSize(size);
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
