using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
//using System.Windows.Forms;
using System.Globalization;


namespace sharpTrade
{
    // un titolo, una collection di serie di dati
    class DBTitle
    {
        private String name;
        public String Name
        {
            get { return name; }
            set { name = value; }
        }

        private LinkedList<String> names;
        private LinkedList<double[]> vals;

        public int SeriesN { get { return Math.Min(names.Count, vals.Count); } }
        public LinkedList<String> SeriesNames { get { return names; } }
        public LinkedList<double[]> SeriesValues { get { return vals; } }

        public int MaxSerieSize
        {
            get
            {
                int max = 0;
                foreach (double[] v in vals)
                    if (v.Length > max)
                        max = v.Length;

                return max;
            }
        }


        public DBTitle(String name)
        {
            this.name = name;

            names = new LinkedList<String>();
            vals = new LinkedList<double[]>();
        }

        public DBTitle(DBTitle val)
        {
            this.name = val.name;
            names = new LinkedList<String>();
            vals = new LinkedList<double[]>();

            for (int i = 0; i < val.names.Count; i++)
                AddSerie(val.names.ElementAt(i), val.vals.ElementAt(i));
        }

        public DBTitle Clone() { return new DBTitle(this); }



        public double[] GetRawData()
        {
            double[] vvv = GetSerieByName("open");
            if (vvv == null) vvv = GetSerieByName("Open");
            if (vvv == null) vvv = GetSerieByName("close");
            if (vvv == null) vvv = GetSerieByName("Close");

            return vvv;
        }


        public String StartDate
        {
            get
            {
                double[] vvv = GetSerieByName("Date");
                if (vvv == null) vvv = GetSerieByName("t_axis");
                if (vvv == null) vvv = GetSerieByName("time");
                if (vvv == null) return "unknown";
                else
                {
                    double val = Math.Min(vvv[0], vvv[vvv.Length - 1]);
                    return val.ToString().Replace(',', '.');
                }
            }
        }

        public String EndDate
        {
            get
            {
                double[] vvv = GetSerieByName("Date");
                if (vvv == null) vvv = GetSerieByName("t_axis");
                if (vvv == null) vvv = GetSerieByName("time");
                if (vvv == null) return "unknown";
                else
                {
                    double val = Math.Max(vvv[0], vvv[vvv.Length - 1]);
                    return val.ToString().Replace(',', '.');
                }
            }
        }


        


        // throws IOException
        public bool Save(StreamWriter SW, String prefix)
        {
            SW.WriteLine(prefix + "<title>");
            SW.WriteLine(prefix + "\t<name>" + name + "</name>");
            SW.WriteLine(prefix + "\t<seriesn>" + names.Count + "</seriesn>");

            for (int i = 0; i < names.Count; i++)
            {
                SW.WriteLine(prefix + "\t<serie>");
                SW.WriteLine(prefix + "\t\t<name>" + names.ElementAt(i) + "</name>");

                double[] vvv = vals.ElementAt(i);

                SW.WriteLine(prefix + "\t\t<valuesn>" + vvv.Length + "</valuesn>");
                SW.WriteLine();
                for (int k=0; k<vvv.Length; k++)
                    SW.WriteLine(prefix + "\t\t<value>" + vvv[k].ToString().Replace(',', '.') + "</value>");


                SW.WriteLine(prefix + "\t\t</serie>");           
            }

            SW.WriteLine(prefix + "</title>");
            return true;
        }




        // throws IOException
        public bool SaveBTD(BinaryWriter SW)
        {
//            SW.Write(name.Length);
            SW.Write(name);
            SW.Write(SeriesN);

            for (int i = 0; i < SeriesN; i++)
            {
//                SW.Write(names.ElementAt(i).Length);
                SW.Write(names.ElementAt(i));
                double[] cs = vals.ElementAt(i);
                SW.Write(cs.Length);
                foreach (double d in cs)
                    SW.Write(d);
            }

            return true;
        }


        public static DBTitle LoadFromTrade(StreamReader SR)
        {
            DBTitle ret = null;
            String line;
            int state = 0;
            int seriesn = 0;
            String seriename = "";
            double[] serievals = null;
            int i = 0;
            NumberFormatInfo ni = (NumberFormatInfo)CultureInfo.InstalledUICulture.NumberFormat.Clone();
            ni.NumberDecimalSeparator = ".";



            while ((line = SR.ReadLine()) != null)
            {
                line = line.Trim();

                if (line.Equals(""))
                    continue;

                else if (state == 1 && line.StartsWith("</title>"))
                    return ret;

                else if (state == 0 && line.StartsWith("<name>"))
                {
                    String sname = line.Replace("<name>", "").Replace("</name>", "").Trim();
                    ret = new DBTitle(sname);
                    state = 1;
                }
                else if (state == 1 && line.StartsWith("<seriesn>"))
                {
                    try { seriesn = Int32.Parse(line.Replace("<seriesn>", "").Replace("</seriesn>", "").Trim()); }
                    catch (FormatException) { return null; }
                }
                else if (state == 1 && line.StartsWith("<serie>"))
                {
                    seriename = "";
                    serievals = null;
                    state = 2;
                }
                else if (state == 2 && line.StartsWith("</serie>"))
                {
                    if (seriename.Equals("") || serievals == null)
                        return null;

                    ret.AddSerie(seriename, serievals);
                    state = 1;
                }
                else if (state == 2 && line.StartsWith("<name>"))
                    seriename = line.Replace("<name>", "").Replace("</name>", "").Trim();
                else if (state == 2 && line.StartsWith("<valuesn>"))
                {
                    int dim;

                    try { dim = Int32.Parse(line.Replace("<valuesn>", "").Replace("</valuesn>", "").Trim()); }
                    catch (FormatException) { return null; }

                    //                    MessageBox.Show("series are: " + dim + "\n" + "name is: " + seriename);
                    serievals = new double[dim];
                    i = 0;
                }
                else if (state == 2 && line.StartsWith("<value>"))
                {
                    double v;

                    try { v = Double.Parse(line.Replace("<value>", "").Replace("</value>", "").Trim(), ni); }
                    catch (FormatException) { return null; }

                    serievals[i++] = v;
                }
//                else
//                    MessageBox.Show(state + " (" + line + ")");

            }

            return null;
        }

        public static DBTitle LoadFromBTD(BinaryReader SR)
        {
            DBTitle ret = null;

            String titlename = SR.ReadString();
            int seriesn = SR.ReadInt32();
            if (titlename.Equals("") || seriesn <= 0)
                return null;

            ret = new DBTitle(titlename);
            for (int k = 0; k < seriesn; k++)
            {
                String seriename = SR.ReadString();
                int valuesn = SR.ReadInt32();

                if (seriename.Equals("") || valuesn <= 0)
                    return null;
                
                double[] newserie = new double[valuesn];

                for (int w = 0; w < valuesn; w++)
                    newserie[w] = SR.ReadDouble();

                ret.AddSerie(seriename, newserie);
            }

            return ret;
        }



        public double[] GetSerieByName(String s)
        {
            for (int i = 0; i < names.Count; i++)
                if (names.ElementAt(i).Equals(s))
                    return vals.ElementAt(i);

            return null;                    // not found
        }

        public bool AddSerie(String s, double[] v)
        {
            if (v == null)
                return false;

            if (GetSerieByName(s) != null)  // already present
                return false;

            else
            {
                names.AddLast(s);
                vals.AddLast(v);
                return true;
            }
        }

        public bool RemoveSerie(String s)
        {
            double[] vvv = GetSerieByName(s);

            if (vvv != null) {
                vals.Remove(vvv);
                names.Remove(s);
                return true;
            }
            else
                return false;              // not found
        }

        public void ClearSeries()
        {
            vals.Clear();
            names.Clear();
        }




        public int IndexOfDate(int d, int m, int y)
        {
            return IndexOfDate(TI.scalar_day(d, m, y));
        }

        public int IndexOfDate(double reqdate)
        {
            double[] v = GetSerieByName("Date");
            if (v == null)
                return -1;

            for (int i = 0; i < v.Length; i++)
                if (v[i] == reqdate)
                    return i;

            return -1;
        }

        public bool HasValueForDate(int d, int m, int y)
        { return (IndexOfDate(d, m, y) >= 0); }

        public bool HasValueForDate(double reqdate)
        { return (IndexOfDate(reqdate) >= 0); }


        public double GetValueByNameAndDate(String name, int d, int m, int y)
        {
            return GetValueByNameAndDate(name, TI.scalar_day(d, m, y));
        }

        public double GetValueByNameAndDate(String name, double reqdate)
        {
            int i = IndexOfDate(reqdate);
            if (i < 0)
                return -1;
            else
                return GetSerieByName(name)[i];
        }

        public double GetRawValueByDate(int d, int m, int y)
        {
            return GetRawValueByDate(TI.scalar_day(d, m, y));
        }

        public double GetRawValueByDate(double reqdate)
        {
            int i = IndexOfDate(reqdate);
            if (i < 0)
                return -1;
            else
            {
                double[] v = GetRawData();
                if (v == null)
                    return -1;
                else
                    return v[i];
            }
        }


        public double GetLastValueByName(String name)
        {
            double[] d = GetSerieByName(name);
            return (d == null ? -1 : d[d.Length-1]);
        }

        public double GetLastRawValue()
        {
            double[] d = GetRawData();
            return (d == null ? -1 : d[d.Length - 1]);
        }
    }
}
