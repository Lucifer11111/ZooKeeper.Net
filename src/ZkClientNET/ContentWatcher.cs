﻿using log4net;
using System;
using System.Threading;
using ZkClientNET.Exceptions;

namespace ZkClientNET
{
    public class ContentWatcher<T> : IZkDataListener
    {
        private static readonly ILog LOG = LogManager.GetLogger(typeof(ContentWatcher<T>));
        private object _contentLock = new object();
        private static AutoResetEvent resetEvent = new AutoResetEvent(false);

        private Holder<T> _content;
        private string _fileName;
        private ZkClient _zkClient;
     
        public ContentWatcher(ZkClient zkClient, string fileName)
        {
            _fileName = fileName;
            _zkClient = zkClient;
        }

        public void Start()
        {
            _zkClient.SubscribeDataChanges(_fileName, this);
            LOG.Debug("Started ContentWatcher");
        }

        private void ReadData()
        {
            try
            {
                SetContent(_zkClient.ReadData<T>(_fileName));
            }
            catch (ZkNoNodeException e)
            {
                // ignore if the node has not yet been created
            }
        }

        public void Stop()
        {
            _zkClient.UnSubscribeDataChanges(_fileName, this);
        }

        private void SetContent(T data)
        {
            lock (_contentLock)
            {
                LOG.Debug("Received new data: " + data);
                _content = new Holder<T>(data);
                resetEvent.Set();
            }        
        }

        public void HandleDataChange(string dataPath, object data)
        {
            SetContent((T)data);
        }

        public void HandleDataDeleted(string dataPath)
        {
            throw new NotImplementedException();
        }

        public T GetContent()
        {
            while (_content == null)
            {
                resetEvent.WaitOne();
            };
            return _content.value;
        }

    }
}