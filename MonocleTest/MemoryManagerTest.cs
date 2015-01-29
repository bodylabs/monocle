using System;
using System.Threading;
using System.Collections.Generic;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Smithers.Serialization;
using Smithers.Reading.FrameData.Mock;

namespace MonocleTest
{
    [TestClass]
    public class MemoryManagerTest
    {
        // Initialized for every test
        MemoryManager _testManager;
        PrivateObject _privateManager;
        Queue<MemoryFrame> _writableQueue;
        Queue<MemoryFrame> _serializableQueue;
        MemoryFrame[] _frameArray;

        // Initialized once 
        static MockLiveFrame _fakeKinectData;
        static MemoryFrame _endFrame;
        static FrameSerializer _serializer;
        const int FRAME_COUNT = 30;

        // Called once on startup
        [ClassInitialize]
        public static void InitializeClass(TestContext context)
        {
            _fakeKinectData = MockLiveFrame.GetFakeLiveFrame();
            _endFrame = new MemoryFrame();
            _serializer = new FrameSerializer();
        }

        // Called after every test has run
        [ClassCleanup]
        public static void CleanupClass()
        {
            _fakeKinectData = null;
            _endFrame.Clear();
            _endFrame = null;
            _serializer = null;
        }

        // Called before every test method
        [TestInitialize]
        public void InitializeTests()
        {
            _testManager = new MemoryManager(FRAME_COUNT);
            // We need this private object in order to introspect on the private fields and methods of
            // the MemoryManager class
            _privateManager = new PrivateObject(_testManager);
            _writableQueue = (Queue<MemoryFrame>)_privateManager
                                                 .GetFieldOrProperty("_writableMemory");
            _serializableQueue = (Queue<MemoryFrame>)_privateManager
                                                     .GetFieldOrProperty("_serializeableFrames");
            _frameArray = (MemoryFrame[])_privateManager
                                         .GetFieldOrProperty("_frames");
        }

        // Called after every test method
        [TestCleanup]
        public void CleanupTests()
        {
            _testManager.Dispose();
            _privateManager = null;
            _writableQueue = null;
            _serializableQueue = null;
            _frameArray = null;
        }

        // Test that the Writable Queue Count is decremented by one once we ask for a buffer
        [TestMethod]
        public void GetFirstWritableBufferTest()
        {
            MemoryFrame frame = _testManager.GetWritableBuffer();

            Assert.AreEqual(_writableQueue.Count,
                            FRAME_COUNT - 1,
                            "Writable Queue Size is wrong after getting one Buffer");
            Assert.IsNotNull(frame, "Frame was null when querying the first writable buffer");
            Assert.ReferenceEquals(frame,
                                   _frameArray[0]);
        }

        // Test that the frames are given back in FIFO - order
        [TestMethod]
        public void GetAllWritableBuffersTest()
        {
            for (int i = 0; i < FRAME_COUNT; ++i)
            {
                Assert.ReferenceEquals(_testManager.GetWritableBuffer(),
                                       _frameArray[i]);
                Assert.AreEqual(_writableQueue.Count,
                                FRAME_COUNT - i - 1,
                                "Writable Queue Count does not match");
            }

            // We have "used" all Writable Buffers, there should be no more buffers to get
            Assert.IsNull(_testManager.GetWritableBuffer());
        }

        // Write Fake Frame to Buffer and check that the buffer contains the fake data
        [TestMethod]
        public void WriteFakeFrameToBufferTest()
        {
            MemoryFrame buffer = _testManager.GetWritableBuffer();
            buffer.Update(_fakeKinectData, _serializer);

            // We dont serialize the depthmapping data so this should be null
            Assert.IsNull(buffer.MappedDepth);
            // The rest should be filled
            Assert.IsNotNull(buffer.Depth);
            Assert.IsNotNull(buffer.BodyIndex);
            Assert.IsNotNull(buffer.Infrared);
            Assert.IsNotNull(buffer.Skeleton);
            Assert.IsNotNull(buffer.Color);
        }

        // Tests the producer/consumer interaction:
        // Configuration is as follows:
        // 30 Buffers
        // 60 Frames are sent
        // Frames are produced every 50ms
        // Serialization takes 100ms
        [TestMethod]
        public void FakeSerializationTest()
        {
            Thread _fakeSerializationThread = new Thread(() =>
                {
                    while (true)
                    {
                        MemoryFrame frameToSerialize = _testManager.GetSerializableFrame();
                        if (frameToSerialize == null)
                        {
                            // No work to do
                            Thread.Sleep(30);
                        } 
                        else if (frameToSerialize == _endFrame)
                        {
                            // All frames have been "serialized"
                            return;
                        }
                        else
                        {
                            // Simulate serialization work
                            // Actual code serializes the frame to disk here
                            Thread.Sleep(100);
                            // Free the frame again
                            _testManager.OnFrameSerialized(frameToSerialize);
                        }
                    }
                });

            Thread _fakeKinectThread = new Thread(() =>
                {
                    int fakeFramesToSend = 60;

                    while (fakeFramesToSend > 0)
                    {
                        // TODO: check for null here
                        MemoryFrame frame = _testManager.GetWritableBuffer();
                        
                        // Actual code calls frame.Update(frameData, serializer) here
                        Thread.Sleep(50);
                        
                        // We´re done writing to the buffer, enqueue it for serialization
                        _testManager.EnqueuSerializationTask(frame);
                        
                        --fakeFramesToSend;
                    }
                    
                    _testManager.EnqueuSerializationTask(_endFrame);
                });


            _fakeSerializationThread.Start();
            _fakeKinectThread.Start();

            _fakeKinectThread.Join();
            _fakeSerializationThread.Join();

            Assert.AreEqual(_serializableQueue.Count,
                            0,
                            "Did not serialize all Frames");
            Assert.AreEqual(_writableQueue.Count,
                            FRAME_COUNT,
                            "Not all frames are writable again");
        }

        // Test that MemoryManager gets initialized properly
        [TestMethod]
        public void InitializationTest()
        {
            for (int i = 1; i < 20; ++i)
            {
                using (MemoryManager manager = new MemoryManager(i)) 
                {
                    PrivateObject privateManager = new PrivateObject(manager);

                    var writableMemoryQueue = (Queue<MemoryFrame>)privateManager
                                                                  .GetFieldOrProperty("_writableMemory");
                    Assert.AreEqual(writableMemoryQueue.Count, 
                                    i, 
                                    "Writable queue size at initialization not correct");


                    var serializeableFrames = (Queue<MemoryFrame>)privateManager
                                                                  .GetFieldOrProperty("_serializeableFrames");
                    Assert.AreEqual(serializeableFrames.Count, 
                                    0,
                                    "Serializeable queue size at initialization not correct");


                    var frameArray = (MemoryFrame[])privateManager
                                                    .GetFieldOrProperty("_frames");
                    Assert.AreEqual(frameArray.Length, 
                                    i,
                                    "Array of MemoryFrames has wrong size");

                    for (int j = 0; j < i; j++)
                    {
                        Assert.IsNotNull(frameArray[j],
                                         "Frame Array was not initialized");
                    }
                }
            }
        }
    }
}
