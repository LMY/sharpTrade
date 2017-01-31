using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;     // DEBUG: solo per MessageBox.Show
using System.Globalization;


namespace sharpTrade
{
    public class Title
    {
        private String name;
        protected double[] open;
        protected double[] close;
        protected double[] high;
        protected double[] low;
        protected double[] adj_close;
        protected double[] volume;
        protected DateTime[] dates;

        protected double[][] ti;
        protected string[] ti_names;

        public String Name { get { return name; } }
        public DateTime[] Date { get { return dates; } }
        public double[] Open { get { return open; } }
        public double[] Close { get { return close; } }
        public double[] High { get { return high; } }
        public double[] Low { get { return low; } }
        public double[] AdjClose { get { return adj_close; } }
        public double[] Volume { get { return volume; } }
        public DateTime Tstart { get { return dates.Length > 0 ? dates[0] : new DateTime(2222, 12, 22); } }
        public DateTime Tend { get { return dates.Length > 0 ? dates[dates.Length-1] : new DateTime(2222, 12, 22); } }
        public int Length { get { return close.Length; } }

        public double[][] TI { get { return ti; } }
        public string[] TI_Name { get { return ti_names; } }

        private int day0;
        public int Day0 { get { return day0; } }




        private Title(String name, int length)
        {
            this.name = name;

            open = new double[length];
            close = new double[length];
            high = new double[length];
            low = new double[length];
            adj_close = new double[length];
            volume = new double[length];
            dates = new DateTime[length];
		
            RemoveTIs();
        }


        public bool AddTI(string name, double[] vals)
        {
            if (vals.Length != Length) // se cerchi di aggiungere una serie lunga diversa dai dati
               return false;           // (e quindi la corrispondenza in t si fotte) VAFFANCULO.
			
			double[][] new_ti = new double[ti.Length+1][];
            string[] new_tinames = new string[ti.Length + 1];
            for (int i = 0; i < ti.Length; i++)
            {
                new_ti[i] = ti[i];
                new_tinames[i] = ti_names[i];
            }
            new_ti[new_ti.Length - 1] = vals;
            new_tinames[new_ti.Length - 1] = name;

            ti = new_ti;
            ti_names = new_tinames;
			
            return true;
        }

        public void RemoveTIs()
        {
            ti = new double[0][];
            ti_names = new string[0];
        }


        public int IndexOfDate(DateTime date)
        {
            for (int i = 0; i < dates.Length; i++)
                if (dates[i].Equals(date))
                    return i;

            return -1;
        }

        public void SetDay0(DateTime t)
        {
            day0 = IndexOfDate(t);
            if (day0 < 0) day0 = 0;
        }


        public bool Cut(DateTime symT0, DateTime symT1,int nTI)
        {
            int ind_start = -1;
            int ind_end = -1;

            for (int i = 0; i < dates.Length; i++)
            {
                if (dates[i].Equals(symT0))
                {
                    ind_start = i - nTI; // per evitare di avere una sfilza di TI = 0
                    
                    if (ind_start < 0)
                        return false;
                }
                if (dates[i].Equals(symT1))
                    ind_end = i;
            }

            if (ind_start < 0 || ind_end < 0 || ind_end - ind_start <= 0)
                return false;

            else if (ind_start != 0 || ind_end != dates.Length - 1)
            {
                open = sharpTrade.TI.sub(open, ind_start, ind_end);
                high = sharpTrade.TI.sub(high, ind_start, ind_end);
                low = sharpTrade.TI.sub(low, ind_start, ind_end);
				close = sharpTrade.TI.sub(close, ind_start, ind_end); // bella lì
                adj_close = sharpTrade.TI.sub(adj_close, ind_start, ind_end);
                volume = sharpTrade.TI.sub(volume, ind_start, ind_end);
				
				//System.Console.WriteLine(Name + ": " + open.Length);

                DateTime[] newdates = new DateTime[ind_end - ind_start + 1];
                for (int i = 0; i < newdates.Length; i++)
                    newdates[i] = dates[i + ind_start];
                dates = newdates;

                for (int x = 0; x < ti.Length; x++) // foreach ti
                    ti[x] = sharpTrade.TI.sub(ti[x], ind_start, ind_end);
            }

            return true;
        }


        public static Title ImportCSV(String filename)
        {
            try
            {
                StreamReader SR = new StreamReader(filename);
                string s;
                int linecount = 0;

                SR.ReadLine();

                while (SR.ReadLine() != null)
                    linecount++;


                Title title = new Title(
                                        filename.Substring(filename.LastIndexOfAny(new char[2]{ '\\', '/'}) + 1,
                                        filename.LastIndexOf('.') - filename.LastIndexOfAny(new char[2] { '\\', '/' }) - 1),
                                        linecount);
                
                SR.Close();
                SR = new StreamReader(filename);
                SR.ReadLine();

                System.Globalization.CultureInfo ci = System.Globalization.CultureInfo.InstalledUICulture;
                NumberFormatInfo ni = (System.Globalization.NumberFormatInfo)
                ci.NumberFormat.Clone();
                ni.NumberDecimalSeparator = ".";


                for (int i = 0; i < linecount; i++)
                {
                    s = SR.ReadLine();
                    string[] values = s.Split(',');

                    title.dates[linecount-1-i] = DateTime.Parse(values[0]); // questa riga non funzionerà mai
                    title.open[linecount - 1 - i] = Double.Parse(values[1], ni);
                    title.high[linecount - 1 - i] = Double.Parse(values[2], ni);
                    title.low[linecount - 1 - i] = Double.Parse(values[3], ni);
                    title.close[linecount - 1 - i] = Double.Parse(values[4], ni);
                    title.volume[linecount - 1 - i] = Double.Parse(values[5], ni);
                    title.adj_close[linecount - 1 - i] = Double.Parse(values[6], ni);
                }

                SR.Close();
                return title;
            }
            catch (Exception) { return null; }
        }
    }



    // L'intero DB, una collection di Titoli
    public class DB
    {
        private Title[] titles;
        public Title[] Titles { get { return titles; } }


        public DB()
        {
            Clear();
        }
		
		public DB(DB database)
		{
			titles = database.Titles;
		}

        public bool ImportCSV(string filename)
        {
            Title[] newtitles = new Title[titles.Length + 1];

            for (int i = 0; i < titles.Length; i++)
                newtitles[i] = titles[i];

            newtitles[newtitles.Length - 1] = Title.ImportCSV(filename);
            return (newtitles[newtitles.Length - 1] != null);
        }

        public void Clear()
        {
            titles = new Title[0]; 
        }

        public bool ImportCSVDir(string filename)
        {
            FileInfo[] files = null;

            try
            {
                DirectoryInfo d = new DirectoryInfo(filename);
                files = d.GetFiles();
            }
            catch (Exception) { return false; }

            titles = new Title[files.Length];
            bool ret = true;

            for (int i = 0; i < titles.Length; i++)
            {
                titles[i] = Title.ImportCSV(files[i].FullName);

                if (titles[i] == null)
                    ret = false;
            }

            Validate();
            return ret;
        }


        public void Purge(DateTime t0, DateTime t1,int nTI)
        {
            bool[] delflag = new bool[titles.Length];
            for (int i = 0; i < delflag.Length; i++)
                delflag[i] = false;


            for (int i = 0; i < titles.Length; i++)
            {
                if (titles[i].Tstart > t0 || titles[i].Tend < t1)
                    delflag[i] = true;

                if (!titles[i].Cut(t0, t1, nTI))
                    delflag[i] = true;
				
				double total =	titles[i].Close[titles[i].Close.Length-1]/titles[i].Close[0];
				if (total > 5)
                    delflag[i] = true;
                    
				if (total < 0.2)
                    delflag[i] = true;
				
				total =	titles[i].High[titles[i].High.Length-1]/titles[i].High[0];
				if (total > 5)
                    delflag[i] = true;
                    
				if (total < 0.2)
                    delflag[i] = true;
				
				total =	titles[i].Low[titles[i].Low.Length-1]/titles[i].Low[0];
				if (total > 5)
                    delflag[i] = true;
                    
				if (total < 0.2)
                    delflag[i] = true;
				

                double[] dperc = TI.calculate_dperc(titles[i].Close, 1);
                for (int k=1; k<dperc.Length; k++)
                    if (Math.Abs(dperc[k]) > 0.5)
                    {
                        delflag[i] = true;
                        break;  
					}
				
				dperc = TI.calculate_dperc(titles[i].High, 1);
                for (int k=1; k<dperc.Length; k++)
                    if (Math.Abs(dperc[k]) > 0.5)
                    {
                        delflag[i] = true;
                        break;  
					}
				
				dperc = TI.calculate_dperc(titles[i].Low, 1);
                for (int k=1; k<dperc.Length; k++)
                    if (Math.Abs(dperc[k]) > 0.5)
                    {
                        delflag[i] = true;
                        break;  
					}
				
				
				double gamma = 6;
				
				for(int k=1;k<10;k++) {
					dperc = TI.calculate_dperc(titles[i].Close, k);
					for (int z=1; z<dperc.Length; z++) {
						if (Math.Abs(dperc[z]) > gamma * TI.SimulationSigmaFTSEraw(k))
						{
							delflag[i] = true;
							break;  
						}
					}
				}
				
            }

            // remove elements and stuff
            int newlength = 0;
            for (int i=0; i<titles.Length; i++)
                if (!delflag[i])
                    newlength++;

            Title[] newtitles = new Title[newlength];
//            MessageBox.Show("Total Purged(): " + newtitles.Length + " over: " + titles.Length);
            int index = 0;

            for (int i = 0; i < titles.Length; i++){
                if (!delflag[i]){
                    titles[i].SetDay0(t0);
                    newtitles[index++] = titles[i];
				} // else System.Console.WriteLine("purged: " + i);
			}
			

            titles = newtitles;
        }




        void Validate()
        {
            bool[] delflag = new bool[titles.Length];
            for (int i = 0; i < delflag.Length; i++)
                delflag[i] = false;

            for (int i = 0; i < titles.Length; i++)
                if (titles[i].Open.Length < 10 ||
                    titles[i].Close.Length < 10 ||
                    titles[i].High.Length < 10 ||
                    titles[i].Low.Length < 10 ||
                    titles[i].Volume.Length < 10 ||
                    titles[i].AdjClose.Length < 10 ||
                    titles[i].Date.Length < 10)
                    delflag[i] = true;



            // remove elements and stuff
            int newlength = 0;
            for (int i=0; i<titles.Length; i++)
                if (!delflag[i])
                    newlength++;

            Title[] newtitles = new Title[newlength];
//            MessageBox.Show("Total Purged(): " + newtitles.Length + " over: " + titles.Length);
            int index = 0;

            for (int i = 0; i < titles.Length; i++)
                if (!delflag[i])
                    newtitles[index++] = titles[i];

            titles = newtitles;
        }
    }
}
