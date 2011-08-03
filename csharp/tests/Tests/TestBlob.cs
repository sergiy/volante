namespace Volante
{
    using System;
    using System.Diagnostics;
    using System.IO;

    public class TestBlob
    {
        public static string FindSrcImplDirectory()
        {
            string dir = Path.Combine("src", "impl");
            if (Directory.Exists(dir))
            {
                return dir;
            }
            dir = Path.Combine("..", dir);
            dir = Path.Combine("..", dir);
            dir = Path.Combine("..", dir);
            dir = Path.Combine("..", dir);
            if (Directory.Exists(dir))
            {
                return dir;
            }
            return null;
        }

        public static string dbName = "testblob.dbs";

        public static void Run()
        {
            Storage db = StorageFactory.CreateStorage();
            db.Open(dbName);
            byte[] buf = new byte[1024];
            int rc;
            string dir = FindSrcImplDirectory();
            string[] files = Directory.GetFiles(dir, "*.cs");
            Index<string, Blob> root = (Index<string, Blob>)db.Root;
            if (root == null)
            {
                root = db.CreateIndex<string, Blob>(true);
                db.Root = root;
                foreach (string file in files)
                {
                    FileStream fin = new FileStream(file, FileMode.Open, FileAccess.Read);
                    Blob blob = db.CreateBlob();
                    Stream bout = blob.GetStream();
                    while ((rc = fin.Read(buf, 0, buf.Length)) > 0)
                    {
                        bout.Write(buf, 0, rc);
                    }
                    root[file] = blob;
                    fin.Close();
                    bout.Close();
                }
            }
            foreach (string file in files)
            {
                byte[] buf2 = new byte[1024];
                Blob blob = root[file];
                Tests.AssertThat(blob != null);
                if (blob == null)
                {
                    Console.WriteLine("File " + file + " not found in database");
                    continue;
                }
                Stream bin = blob.GetStream();
                FileStream fin = new FileStream(file, FileMode.Open, FileAccess.Read);
                while ((rc = fin.Read(buf, 0, buf.Length)) > 0)
                {
                    int rc2 = bin.Read(buf2, 0, buf2.Length);
                    Tests.AssertThat(rc == rc2);
                    if (rc != rc2)
                    {
                        Console.WriteLine("Different file size: " + rc + " .vs. " + rc2);
                        break;
                    }
                    while (--rc >= 0 && buf[rc] == buf2[rc]) ;
                    Tests.AssertThat(rc < 0);
                    if (rc >= 0)
                    {
                        Console.WriteLine("Content of the files is different: " + buf[rc] + " .vs. " + buf2[rc]);
                        break;
                    }
                }
                fin.Close();
                bin.Close();
            }
            db.Close();
        }
    }

}