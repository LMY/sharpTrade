
// from sharpBW

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System.Threading;
using System.Windows.Forms;



namespace sharpTrade
{
    class TaskList
    {
        private LinkedList<Task> tasks;
        private Form1 frame;
        private bool running;
        public bool Running { get { return running; } }

        public TaskList(Form1 frame)
        {
            this.frame = frame;
            tasks = new LinkedList<Task>();
            running = false;
        }

        public void notifyState(Task task, String state, double perc)
        {
            if (state == "STOP")
            {
                if (!tasks.Remove(task))
                    MessageBox.Show("Something really bad happened with threads...");

                frame.NotifyNewFile(task.Filename);
                frame.notifyStatus("DONE", task.Url, (double)tasks.Count);

                // fanne partire uno nuovo
                if (tasks.Count > 0 && running)
                    tasks.First.Value.Start();
                else
                    frame.notifyStatus("STANDBY", "", 0);
            }
            else if (state == "PROGRESS")
            {
                if (tasks.First.Value.Equals(task))
                    frame.notifyStatus("PROGRESS", task.Url, perc);
            }
        }


        public void newTaskSingleFile(String url)
        {
            DirectoryInfo dir = new DirectoryInfo(Config.Instance.IncomingDirectory);
            if (!dir.Exists) {
                try {
                    dir.Create();
                }
                catch (IOException) { return; }
            }

            String filename = getFilenameFromUrl(url);
            FileInfo finfo = new FileInfo(Config.Instance.IncomingDirectory + filename);
            if (finfo.Exists)
                //filename = Utility.findAlternativeFilename(filename, Config.Instance.IncomingDirectory);
                return;

            newTaskFile(url, Config.Instance.IncomingDirectory + filename, -1);
        }


        public void newTaskFile(String url, String filename, int idref)
        {
            Task t = new TaskFile(this, url, filename, idref);

            if (tasks.Count == 0)
            {
                tasks.AddFirst(t);
                if (running)
                    t.Start();
            }
            else if (!running)
            {
                tasks.AddLast(t);
                return;
            }
            else
            {
                LinkedListNode<Task> node = tasks.First;
                while (node.Value.Running)
                {
                    node = node.Next;

                    if (node == null)
                    {
                        tasks.AddLast(t);
                        return;
                    }
                }

                tasks.AddBefore(node, t);
            }
        }



        public void Start()
        {
            if (!running)
            {
                running = true;
                if (tasks.Count > 0)
                    tasks.First.Value.Start();
            }
        }


        public void Resume()
        {
            if (!running)
            {
                running = true;

                if (tasks.Count > 0)
                    tasks.First.Value.Start();
            }
        }


        public void Pause()
        {
            running = false;

            int i = 0;

            while (i != tasks.Count)
            {
                Task t = tasks.ElementAt(i);

                if (t.Running)
                {
                    try { t.forceStop(); }
                    catch (ThreadAbortException) { }
                    Task clonedTask = t.Clone();
                    tasks.Remove(t);
                    tasks.AddFirst(clonedTask);
                }
                else
                    i++;
            }

            frame.notifyStatus("PAUSED", "", (double)tasks.Count);
        }

        public void Abort()
        {
            Pause();
            tasks.Clear();

            frame.notifyStatus("ABORTED", "", (double)tasks.Count);
        }

        public static String getFilenameFromUrl(String url)
        {
            int last_slash = url.LastIndexOf('/');
            if (last_slash < 0)
                return "no_name";
            else
                return url.Substring(last_slash + 1);
        }
    }



    //
    // Task classes
    //

    abstract class Task
    {
        public const int PACKET_LENGTH = 4096;

        protected TaskList tasklist;

        protected String url;
        public String Url { get { return url; } }

        protected String filename;
        public String Filename
        {
            get { return filename; }
            set { filename = value; }
        }


        protected bool running;
        public bool Running { get { return running; } }

        private Thread t;


        protected int task_id;
        protected int task_idref;
        public int TaskId { get { return task_id; } }
        public int TaskIdref { get { return task_idref; } }
        protected static int task_idnext = 0;



        public Task(TaskList tasklist, String url, String filename, int task_idref)
        {
            this.tasklist = tasklist;
            this.url = url;
            this.filename = filename;
            this.task_id = task_idnext++;
            this.task_idref = task_idref;


            running = false;
            t = new Thread(startThread);
        }

        public Task(Task task)
            : this(task.tasklist, task.url, task.filename, task.TaskIdref)
        {
        }




        public void Start()
        {
            running = true;
            t.Start();
        }

        public void requestStop()   { running = false; }
        public void forceStop()
        {
            requestStop();
            t.Abort();
        }


        protected void startThread()
        {
            tasklist.notifyState(this, "START", 0);
            DoWork();
            tasklist.notifyState(this, "STOP", 0);
        }


        public abstract Task Clone();
        public abstract void DoWork();


        // please realize that by calling this method you'll save a text file, fcknfckinsdking up all the fuckin file content
        public void ToFile(String data)
        {
            try
            {
                StreamWriter SW = File.CreateText(filename);
                SW.Write(data);
                SW.Close();
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.ToString(), "Error writing " + filename, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }


        public String GetData(bool save_binary)
        {
            StringBuilder data = new StringBuilder();

            FileStream wr = null;

            try
            {
                if (url.StartsWith("file://"))
                {
                    StreamReader re = File.OpenText(url.Substring(7));
                    string input = null;
                    while ((input = re.ReadLine()) != null)
                        data.AppendLine(input);
                    re.Close();
                }
                else
                {
                    if (save_binary)
                    {
                        DirectoryInfo incoming = new DirectoryInfo(Config.Instance.IncomingDirectory);
                        if (!incoming.Exists)
                            incoming.Create();
                        wr = new FileStream(filename, FileMode.OpenOrCreate, FileAccess.Write);
                    }

                    WebRequest request = WebRequest.Create(url);
                    request.Timeout = 5000; // msec
                    HttpWebResponse response;
                    try { response = (HttpWebResponse)request.GetResponse(); }
                    catch (WebException) { return ""; }                    // ignore errors.


                    long length = response.ContentLength;

                    if (response.StatusCode != HttpStatusCode.OK)
                        return "";

                    Stream dataStream = response.GetResponseStream();
                    byte[] buffer = new byte[PACKET_LENGTH];
                    int dim = 0, current = 0;

                    do
                    {
                        dim = dataStream.Read(buffer, 0, buffer.Length);

                        if (wr != null && dim > 0)
                            wr.Write(buffer, 0, dim);

                        for (int i = 0; i < dim; i++)
                            data.Append((char)buffer[i]);

                        current += dim;
                        tasklist.notifyState(this, "PROGRESS", (double)current / length);
                    }
                    while (running && dim > 0);

                    dataStream.Close();
                    response.Close();
                }
            }
            catch (Exception exception)
            {
                if (exception is ThreadAbortException)
                    throw exception;

// Some kind of error occured. most likely something server side, so just skip this file
//                MessageBox.Show(exception.ToString(), "ERROR: " + url, MessageBoxButtons.OK);
                return "";
            }
            finally
            {
                if (wr != null)
                {
                    wr.Close();
                    wr = null;
                }
            }

            return data.ToString();
        }
    }


    // till refactoring, calling TaskFile::ToFile() will destroy the file (filename)
    class TaskFile : Task
    {
        public TaskFile(TaskList tasklist, String url, String filename, int task_idref)
            : base(tasklist, url, filename, task_idref)
        {}

        public TaskFile(TaskFile task)
            : this(task.tasklist, task.url, task.filename, task.TaskIdref)
        {}

        public override void DoWork()
        {
            GetData(true);
        }

        public override Task Clone()
        {
            return new TaskFile(this);
        }
    }
}
