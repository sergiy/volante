using System;
using NachoDB;

public class UnitTestsRunner
{
    public static void Main(string[] args)
    {
        TestBit.Run(100);
        UnitTests.SafeDeleteFile(TestBlob.dbName);
        TestBlob.Run();
        TestBlob.Run();
        TestXml.Run(100, false);
        TestXml.Run(100, true);
        Test1.Run(false);
        Test1.Run(true);
        Test2.Run(false);
        Test2.Run(true);
        if (0 == UnitTests.FailedTests)
        {
            Console.WriteLine(String.Format("OK! All {0} tests passed", UnitTests.TotalTests));
        }
        else
        {
            Console.WriteLine(String.Format("FAIL! Failed {0} out of {1} tests", UnitTests.FailedTests, UnitTests.TotalTests));
        }
    }
}

