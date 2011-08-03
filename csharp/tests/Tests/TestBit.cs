namespace Volante
{
    using System;
    using System.Diagnostics;

    public class TestBit
    {
        [Flags]
        public enum Options
        {
            CLASS_A = 0x00000001,
            CLASS_B = 0x00000002,
            CLASS_C = 0x00000004,
            CLASS_D = 0x00000008,

            UNIVERAL = 0x00000010,
            SEDAN = 0x00000020,
            HATCHBACK = 0x00000040,
            MINIWAN = 0x00000080,

            AIR_COND = 0x00000100,
            CLIMANT_CONTROL = 0x00000200,
            SEAT_HEATING = 0x00000400,
            MIRROR_HEATING = 0x00000800,

            ABS = 0x00001000,
            ESP = 0x00002000,
            EBD = 0x00004000,
            TC = 0x00008000,

            FWD = 0x00010000,
            REAR_DRIVE = 0x00020000,
            FRONT_DRIVE = 0x00040000,

            GPS_NAVIGATION = 0x00100000,
            CD_RADIO = 0x00200000,
            CASSETTE_RADIO = 0x00400000,
            LEATHER = 0x00800000,

            XEON_LIGHTS = 0x01000000,
            LOW_PROFILE_TIRES = 0x02000000,
            AUTOMATIC = 0x04000000,

            DISEL = 0x10000000,
            TURBO = 0x20000000,
            GASOLINE = 0x40000000,
        }

        class Car : Persistent
        {
            internal int hps;
            internal int maxSpeed;
            internal int timeTo100;
            internal Options options;
            internal string model;
            internal string vendor;
            internal string specification;
        }

        class Catalogue : Persistent
        {
            internal FieldIndex<string, Car> modelIndex;
            internal BitIndex<Car> optionIndex;
        }

        public static void Run(int nRecords)
        {
            int pagePoolSize = 48 * 1024 * 1024;
            string dbName = "testbit.dbs";

            Tests.SafeDeleteFile(dbName);
            Storage db = StorageFactory.CreateStorage();
            db.Open(dbName, pagePoolSize);

            Catalogue root = (Catalogue)db.Root;
            Tests.AssertThat(root == null);
            root = new Catalogue();
            root.optionIndex = db.CreateBitIndex<Car>();
            root.modelIndex = db.CreateFieldIndex<string, Car>("model", true);
            db.Root = root;

            DateTime start = DateTime.Now;
            long rnd = 1999;
            int i, n;

            Options selectedOptions = Options.TURBO | Options.DISEL | Options.FWD | Options.ABS | Options.EBD | Options.ESP | Options.AIR_COND | Options.HATCHBACK | Options.CLASS_C;
            Options unselectedOptions = Options.AUTOMATIC;

            for (i = 0, n = 0; i < nRecords; i++)
            {
                rnd = (3141592621L * rnd + 2718281829L) % 1000000007L;
                Options options = (Options)rnd;
                Car car = new Car();
                car.hps = i;
                car.maxSpeed = car.hps * 10;
                car.timeTo100 = 12;
                car.vendor = "Toyota";
                car.specification = "unknown";
                car.model = Convert.ToString(rnd);
                car.options = options;
                root.modelIndex.Put(car);
                root.optionIndex[car] = (int)options;
                if ((options & selectedOptions) == selectedOptions && (options & unselectedOptions) == 0)
                {
                    n += 1;
                }
            }
            Console.WriteLine("Elapsed time for inserting " + nRecords + " records: "
                + (DateTime.Now - start));

            start = DateTime.Now;
            i = 0;
            foreach (Car car in root.optionIndex.Select((int)selectedOptions, (int)unselectedOptions))
            {
                Debug.Assert((car.options & selectedOptions) == selectedOptions);
                Debug.Assert((car.options & unselectedOptions) == 0);
                i += 1;
            }
            Console.WriteLine("Number of selected cars: " + i);
            Debug.Assert(i == n);
            Console.WriteLine("Elapsed time for bit search through " + nRecords + " records: "
                + (DateTime.Now - start));

            start = DateTime.Now;
            i = 0;
            foreach (Car car in root.modelIndex)
            {
                root.optionIndex.Remove(car);
                car.Deallocate();
                i += 1;
            }
            Debug.Assert(i == nRecords);
            root.optionIndex.Clear();
            Console.WriteLine("Elapsed time for removing " + nRecords + " records: "
                + (DateTime.Now - start));

            db.Close();
        }
    }
}