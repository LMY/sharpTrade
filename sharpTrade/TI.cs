using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Globalization;



namespace sharpTrade
{
    class TI
    {
        private static DateTime day_0;
        public static DateTime Day0 { get { return day_0; } }

        private static bool inizialized = false;
        public static bool Initialized { get { return inizialized; } }

        public static void init()
        {
            day_0 = new DateTime(2000, 1, 1);
            inizialized = true;
        }
		
		public static double scalar_day(int d, int m, int y)
        {
            return scalar_day(new DateTime(y, m, d));
        }

        public static double scalar_day(DateTime reqdate)
        {
            return reqdate.Subtract(day_0).TotalDays;
        }

        public static DateTime scalar_invday(double span)
        {
            return day_0.Add(TimeSpan.FromDays(span));
        }
		
		
		
		
        public static double[] calculate_d(double[] f)
        { return calculate_d(f, 1); }
		
		public static double[] calculate_d2(double[] f)
        { return calculate_d2(f, 1); }
		
		 public static double[] calculate_dperc(double[] f)
        { return calculate_d(f, 1); }
		
	
		// derivata prima
        public static double[] calculate_d(double[] f, int n)
        {
            if (f == null  ||  f.Length == 0)
                return null;

            double[] ret = new double[f.Length];

            for (int i = 0; i < n; i++)
                ret[i] = 0;

            for (int i = n; i < f.Length; i++)
                ret[i] = (f[i] - f[i - n])/n;


            return ret;
        }

		// derivata seconda
        public static double[] calculate_d2(double[] f, int n)
        {
            if (f == null || f.Length == 0)
                return null;

            double[] ret = new double[f.Length];

            for (int i = 0; i < 2*n; i++)
                ret[i] = 0;

            for (int i = 2*n; i < f.Length; i++)
                ret[i] = (f[i] - 2*f[i - n] + f[i - 2*n])/Math.Pow(n, 2);


            return ret;
        }
		
		// variazione percentuale n giorni precedenti
        public static double[] calculate_dperc(double[] f, int n)
        {
            if (f == null || f.Length == 0)
                return null;

            double[] ret = new double[f.Length];

            for (int i = 0; i < n; i++)
                ret[i] = 0;

            for (int i = n; i < f.Length - 2; i++)
                ret[i] = (f[i] - f[i - n])/f[i];


            return ret;
        }

		// media mobile (lineare)
        public static double[] calculate_mm(double[] values, int n)
        {
            double[] w = new double[n];
            for (int i = 0; i < n; i++)
                w[i] = 1;

            return calculate_mm_gen(values, n, w);
        }

		// media mobile (esponenziale)
        public static double[] calculate_emm(double[] values, int n, double alpha) // alpha < 1
        {
            double[] w = new double[n];
            for (int i = 0; i < n; i++)
                w[i] = Math.Pow(alpha, i);

            return calculate_mm_gen(values, n, w);
        }

		// media mobile (generalizzata)
        private static double[] calculate_mm_gen(double[] values, int n, double[] w)
        {
            double[] newvalues = new double[values.Length];

            double[] norm = new double[n];
            norm[0] = w[0];
            for (int i = 1; i < n; i++)
                norm[i] = norm[i - 1] + w[i];

            double[] past = new double[n];
            for (int i = 0; i < n; i++)
                past[i] = 0;

            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < n; j++)
                    past[j] += values[i] * w[j];

                newvalues[i] = past[0] / norm[i < n ? i : n - 1];

                // shift left
                for (int k = 0; k < n - 1; k++)
                    past[k] = past[k + 1];
                past[n - 1] = 0;
            }

            return newvalues;
        }
		public static double[] calculate_mm_base_new(double[] values, int n, double _base)
		{
			if (n==1) return values;
			
			int len = values.Length;
			double[] newvalues = new double[len];
			for(int i=0;i<len;i++)
				newvalues[i] = 0;

            double[] past = new double[n];
            for (int i = 0; i < n; i++)
                past[i] = 0;

			
			double normalize=0;
			for(int i = 0;i<n;i++)
				normalize+= 1/Math.Pow(_base,i+1);

            for (int i = 0; i < len; i++)
            {
                for (int j = 0; j < n; j++){
					past[j] += values[i];
					past[j] /= _base;
				}
				
                newvalues[i] = past[0]/normalize;

                // shift left
                for (int k = 0; k < n - 1; k++)
                    past[k] = past[k + 1];
				
                past[n - 1] = 0;
            }
			
			for(int i=0;i<n;i++)
				newvalues[i] = 0;

            return newvalues;
			
		}
		
		
		
        public static double[] calculate_mm_base(double[] values, int n, double _base)
        {
			int len = values.Length;
			double[] ret = new double[len];
			for(int i=0;i<len;i++)
				ret[i] = 0;

			double normalize = (Math.Pow(_base,n)-1);
			if (_base == 1) normalize = n;
			
			for(int i=n;i<len;i++){
				for(int j=0;j<n;j++)
					ret[i] += (values[i-j] * Math.Pow(_base,n-j-1));
			
				ret[i]/=normalize;
			}
			
			return ret;
		}

		// deviazione standard (lineare)
        public static double[] calculate_sigma(double[] values, int n)
        {
            double[] w = new double[n];
            for (int i = 0; i < n; i++)
                w[i] = 1;

            return calculate_sigma_gen(values, n, w);
        }

		// deviazione standard (esponenziale)
        public static double[] calculate_esigma(double[] values, int n, double alpha)
        {
            double[] w = new double[n];
            for (int i = 0; i < n; i++)
                w[i] = Math.Pow(alpha, i);

            return calculate_sigma_gen(values, n, w);
        }

		// deviazione standard (generalizzata)
        private static double[] calculate_sigma_gen(double[] values, int n, double[] w) // ok
        {
            double[] newvalues = new double[values.Length];

            double[] norm = new double[n];
            norm[0] = w[0];
            for (int i = 1; i < n; i++)
                norm[i] = norm[i - 1] + w[i];

            double[] past = new double[n];
            double[] past2 = new double[n];
            for (int i = 0; i < n; i++)
            {
                past[i] = 0;
                past2[i] = 0;
            }

            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    past[j] += values[i] * w[j];
                    past2[j] += Math.Pow(values[i] * w[j], 2);
                }

                double ex = past[0] / norm[i < n ? i : n - 1];
                double ex2 = past2[0] / norm[i < n ? i : n - 1];
                newvalues[i] = Math.Sqrt(ex2 - Math.Pow(ex, 2));

                // shift left
                for (int k = 0; k < n - 1; k++)
                {
                    past[k] = past[k + 1];
                    past2[k] = past2[k + 1];
                }
                past[n - 1] = 0;
                past2[n - 1] = 0;
            }

            return newvalues;
        }

		// banda di Bolinger superiore
        public static double[] calculate_BBhigh(double[] values, int n, double lambda)
        {
            double[] newvalues = calculate_mm(values, n);
            double[] valuesvar = calculate_sigma(values, n);

            for (int i = 0; i < values.Length; i++){
				
                newvalues[i] += lambda * valuesvar[i];
			}

            return newvalues;
        }
		
		// banda di Bolinger inferiore
        public static double[] calculate_BBlow(double[] values, int n, double lambda)
        {
            double[] newvalues = calculate_mm(values, n);
            double[] valuesvar = calculate_sigma(values, n);

            for (int i = 0; i < values.Length; i++){
//				System.Console.WriteLine(values[i] + " " + newvalues[i] + " " + valuesvar[i] + " " + newvalues[i]+lambda*valuesvar[i]);
                newvalues[i] -= lambda * valuesvar[i];
				
			}

            return newvalues;
        }

		
		// Indicatore stocastico K
        public static double[] calculate_stochasticK(double[] p, double[] pmax, double[] pmin)
        {
			double[] newvalues = new double[p.Length];
			
            for (int i = 0; i < p.Length; i++)
            {
				newvalues[i] = 100 * (p[i] - pmin[i])/(pmax[i]-pmin[i]);
			}

            return newvalues;
        }
	
		// Indicatorie stocastico D (media mobile lineare di K)
		public static double[] calculate_stochasticD(double[] f, int n)
        {
			double[] newvalues = new double[f.Length];
			newvalues = calculate_mm(f,n);
			return newvalues;
        }
		
		// Indicatorie stocastico D (media mobile esponenziale di K)
		public static double[] calculate_stochasticD(double[] f, int n, int alpha) // alpha < 1
        {
			
			double[] newvalues = new double[f.Length];
			newvalues = calculate_emm(f,n,alpha);
			return newvalues;
        }
		
		
		


		// Relative Strength Index (contando il # di variazioni positive e negative)
        public static double[] calculate_RSIC(double[] values, int n)
        {
            double[] newvalues = new double[values.Length];
			double zero = Math.Pow(10,-4);

            double[] pastp = new double[n];
            double[] pastm = new double[n];
            for (int i = 0; i < n; i++)
            {
                pastp[i] = zero;
                pastm[i] = zero;
            }

            newvalues[0] = 0;
            for (int i = 1; i < values.Length; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    if (values[i] > values[i - 1])
                        pastp[j]++;
                    if (values[i] < values[i - 1])
                        pastm[j]++;
                }

                newvalues[i] = 100 - 100/(1+pastp[0]/pastm[0]);

                // shift left
                for (int k = 0; k < n - 1; k++)
                {
                    pastp[k] = pastp[k + 1];
                    pastm[k] = pastm[k + 1];
                }
                pastp[n - 1] = zero;
                pastm[n - 1] = zero;
            }

            return newvalues;
        }


		// Relative Strength Index (contando l'entità delle variazioni positive e negative) 
        public static double[] calculate_RSIV(double[] values, int n)
        {
            double[] newvalues = new double[values.Length];
			double zero = Math.Pow(10,-4);
			
            double[] pastp = new double[n];
            double[] pastm = new double[n];
            for (int i = 0; i < n; i++)
            {
                pastp[i] = zero;
                pastm[i] = zero;
            }

            newvalues[0] = 0;
            for (int i = 1; i < values.Length; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    if (values[i] > values[i - 1])
                        pastp[j] += values[i]/values[i-1]-1;
                    if (values[i] < values[i - 1])
                        pastm[j] -= values[i]/values[i - 1]-1;            // negative.
                }

                newvalues[i] = 100 - 100/(1+pastp[0]/pastm[0]);

                // shift left
                for (int k = 0; k < n - 1; k++)
                {
                    pastp[k] = pastp[k + 1];
                    pastm[k] = pastm[k + 1];
                }
                pastp[n - 1] = zero;
                pastm[n - 1] = zero;
            }

            return newvalues;
        }
		
		public static double[] min(double[] data,int n)
		{
			double[] newvalues = new double[data.Length];
			
			for(int i =0;i<n;i++)
			{
				newvalues[i]=0;
			}
			
			for(int i=n;i<data.Length;i++)
			{
				newvalues[i] = data[i];
				for(int j=1;j<n;j++)
				{
					newvalues[i] = Math.Min(newvalues[i],data[i-j]);
				}
			}
			
			
			return newvalues;
		}




		public static double[] sub(double[] data, int begin, int end)
		{
            double[] res = new double[end - begin + 1];

            for (int i = 0; i < res.Length; i++)
				res[i] = data[i+begin];

            return res;
		}
		
		
		// Media di un campione
        public static double scalar_mean(double[] values)
        {
            double sum = 0;
            
            for (int i = 0; i < values.Length; i++)
                sum += values[i];
            
            return (sum / values.Length);
        }


		// Deviazione standard di un campione
        public static double scalar_sigma(double[] values)
        {
            double sum = 0;
            double sum2 = 0;


            for (int i = 0; i < values.Length; i++)
            {
                sum += values[i];
                sum2 += Math.Pow(values[i], 2);
            }

            sum /= values.Length;
            sum2 /= values.Length;

            return Math.Sqrt(sum2 - Math.Pow(sum, 2));
        }

			
		//  Minimo, Massimo, Media, Deviazione Standard di un campione
		public static int[] stats(double[] data)
		{
			int len = data.Length;
			double[] res = new double[4]; // min,max,mean,sigma
			double min = data[0];
			double max = data[0];
			double sum = data[0];
			double sum2 = Math.Pow(data[0],2);
			double current;
			double current2;
			for(int i=1;i<len;i++)
			{
				current = data[i];
				current2 = Math.Pow(data[i],2);
				if(current>max)
					max = current;
				
				if(current<min)
					min = current;
				
				sum += current;
                sum2 += current2;
			}
			res[0] = min;
			res[1] = max;
			res[2] = sum/len;
			sum /= len;
            sum2 /= len;
			res[3] = Math.Sqrt(sum2 - Math.Pow(sum, 2));
			int[] ret = new int[4];
			for(int i=0;i<4;i++){
				ret[i] = (int) res[i];
			}
			return ret;
		}
		
				
		// Trasla in giù l'array (al posto del valore di oggi mette quello di ieri)
		public static double[] trasla_down(double[] data, int n)
		{
			int len = data.Length;
			double[] res = new double[len];
			for(int i=0;i<n;i++){
				res[i] = 0;
			}
			for(int i=n;i<len;i++){
				res[i]=data[i-n];
			}
			return res;
		}
		
		// Trasla in su l'array (al posto del valore di oggi mette quello di domani)
		public static double[] trasla_up(double[] data, int n)
		{
			int len = data.Length;
			double[] res = new double[len];
			for(int i=0;i<len-n;i++){
				res[i] = data[i+n];
			}
			for(int i=len-n;i<len;i++){
				res[i] = 0;
			}
			return res;
		}
		
		public static double[] divide(double[] data1,double[] data2)
		{
			if (data1.Length!=data2.Length) return null;
			
			double[] ret = new double[data1.Length];
			
			for(int i=0;i<data1.Length;i++)
				if (data2[i]!=0) 
					ret[i] = data1[i]/data2[i];
			
			return ret;
		}
		
		public static double[] multiply(double[] data,double val)
		{
			double[] ret = new double[data.Length];
			
			for(int i=0;i<data.Length;i++)
				ret[i] = data[i]*val;
			
			return ret;
		}
		
		
		// Da rivedere
		public static double SimulationSigmaFTSEraw(int index)
		{
			if (index > 9) return -1;
			double[] sigma = {0 , 0.0228 , 0.0323 , 0.0398 , 0.0458 , 0.0514 , 0.0562 , 0.0606 , 0.0646 , 0.0685};
			return sigma[index];
		}
		
		public static double SimulationSigmaALL(int index)
		{
			if (index < 0) return -1;
			
			double[] sigma = { 0 , 0.0191 , 0.0261 , 0.0317 , 0.0364 , 0.0407 , 0.0446 , 0.0482 , 0.0516 , 0.0547 , 0.0577 };
			if (index <= 9) return sigma[index];
			
			double a = 0.0185111;
			double b = 0.00459048;
			double c = -8.44635 * Math.Pow(10,-5);
			double d = 7.40389 * Math.Pow(10,-7);
			return  a + b*index + c*Math.Pow(index,2) + d*Math.Pow(index,3);
		}
	}
}