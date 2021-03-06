#region Apache License, Version 2.0
//
// Licensed to the Apache Software Foundation (ASF) under one
// or more contributor license agreements.  See the NOTICE file
// distributed with this work for additional information
// regarding copyright ownership.  The ASF licenses this file
// to you under the Apache License, Version 2.0 (the
// "License"); you may not use this file except in compliance
// with the License.  You may obtain a copy of the License at
//
//   http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing,
// software distributed under the License is distributed on an
// "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
// KIND, either express or implied.  See the License for the
// specific language governing permissions and limitations
// under the License.
//
#endregion
using System;
using System.IO;
using log4net;

namespace NPanday.VisualStudio.Addin
{
    public class WebServicesReferenceWatcher
    {
        public event EventHandler<WebReferenceEventArgs> Renamed;
        public event EventHandler<WebReferenceEventArgs> Created;
        public event EventHandler<WebReferenceEventArgs> Deleted;

        private static readonly ILog log = LogManager.GetLogger(typeof(WebServicesReferenceWatcher));

        FileSystemWatcher watcher;
        string folderPath;

        public WebServicesReferenceWatcher(string folderPath)
        {
            this.folderPath = folderPath;
            this.init();
        }

        public void Start()
        {
            watcher.EnableRaisingEvents = true;
        }

        public void Stop()
        {
            watcher.EnableRaisingEvents = false;
            watcher.Dispose();
        }

        void init()
        {
            
            watcher = new FileSystemWatcher(folderPath);
            watcher.NotifyFilter = NotifyFilters.DirectoryName;
            watcher.Renamed += new RenamedEventHandler(watcher_Renamed);
            watcher.Deleted += new FileSystemEventHandler(watcher_Deleted);
            watcher.Created += new FileSystemEventHandler(watcher_Created);
            watcher.Error += new ErrorEventHandler(watcher_Error);
            watcher.Changed += new FileSystemEventHandler(watcher_Changed);
            watcher.IncludeSubdirectories = false;
            
        }

        void watcher_Changed(object sender, FileSystemEventArgs e)
        {
            log.Debug("web service changed: " + e.FullPath);
        }

        void watcher_Error(object sender, ErrorEventArgs e)
        {
            this.Stop();
        }

        void watcher_Created(object sender, FileSystemEventArgs e)
        {
            WebReferenceEventArgs a = new WebReferenceEventArgs(e.ChangeType, e.FullPath, e.Name);

            onCreated(a);
        }

        void watcher_Deleted(object sender, FileSystemEventArgs e)
        {
            WebReferenceEventArgs a = new WebReferenceEventArgs(e.ChangeType, e.FullPath, e.Name);
            onDeleted(a);
        }

        void watcher_Renamed(object sender, RenamedEventArgs e)
        {
            WebReferenceEventArgs a = new WebReferenceEventArgs(e.ChangeType, e.FullPath, e.Name);
            a.OldNamespace = e.OldName;
            
            onRenamed(a);            
        }

        void onRenamed(WebReferenceEventArgs e)
        {
            if (Renamed != null)
            {
                Renamed(this, e);
            }
        }

        void onDeleted(WebReferenceEventArgs e)
        {
            if (Deleted != null)
            {
                Deleted(this, e);
            }
        }

        void onCreated(WebReferenceEventArgs e)
        {
            if (Created != null)
            {
                Created(this, e);
            }
        }



    }
}