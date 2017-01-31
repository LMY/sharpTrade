using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;


namespace sharpTrade
{
    public enum PositionState { OPENED = 0, CLOSED = 1, NULL = 2 };

    public class Position
    {
        public PositionState State { 
            get {
                if (t_close <0 && t_open >= 0)
                    return PositionState.OPENED;
                else if (t_close >= 0 && t_open >= 0)
                    return PositionState.CLOSED;
                else
                    return PositionState.NULL;
            }
        }

        private int id;
        public int ID { get { return id; } set { id = value; } }

        private int quantity;
        public int Quantity { get { return quantity; } set { quantity = value; } }

        public double Gain { get { return quantity * (title.Close[t_close] - title.Close[t_open]); } }
        public int Delta_t { get { return (t_close - t_open); } }

        public double Value(int t)
        {
            return quantity * title.Close[t];
        }

        private int t_open;
        private int t_close;
        public int TOpen { get { return t_open; } set { t_open = value; } }
        public int TClose { get { return t_close; } set { t_close = value; } }

        private Title title;
		public Title Title { get { return title; } set { title = value; } }

        public Position(Title title, int _id, int _quantity, int _topen)
        {
            id = _id;
            quantity = _quantity;
            t_open = _topen;
            t_close = -1;

            this.title = title;
        }

        public double Close(int _tclose)
        {
            t_close = _tclose;
            return Value(_tclose);
        }
		
		public bool Equals(Position pos)
		{
			return ((this.ID == pos.ID) && (this.TOpen == pos.TOpen) && (this.Quantity == pos.Quantity) && (this.TClose == pos.TClose));
		}
			
    }



    public class PositionDB
    {
        private LinkedList<Position> opened;
        public LinkedList<Position> OpenedPositions { get { return opened; } }

        private LinkedList<Position> closed;
        public LinkedList<Position> ClosedPositions { get { return closed; } }

        public PositionDB()
        {
            closed = new LinkedList<Position>();
            opened = new LinkedList<Position>();
        }


        public bool Open(Position pos)
        {
			
            foreach (Position p in opened)  // se è già aperta, non fare niente
				if (pos.Equals(p))
					return false;
		
            opened.AddLast(pos);
            return true;
        }

        public double Close(Position pos, int t)
        {
            LinkedListNode<Position> current = opened.First;

            while (current != null)
            {
				if (current.Value.Equals(pos))
                {
                    opened.Remove(current);
                    break;
                }

                current = current.Next;
            }

            double ret = pos.Close(t);
            closed.AddLast(pos);

            return ret;
        }
    }


    public class RunResults
    {
		public double accantonamento;
		public double taxes;
		public double commission;
		
		public double total_investment;
        public double total_gain;
		public double delta_t;
		public double perc_gain;
		public double perc_gain_sigma;	
		
        public double ops_good;
        public double ops_bad;
        public double ops_tot;
		
		public double ops_very_good;
        public double ops_very_bad;

		public double ops_good_gain;
		public double ops_bad_gain;
		
		public double ops_very_good_gain;
		public double ops_very_bad_gain;
		
			
		public double[] title_total_investment;
		public double[] title_total_gain;
		public double[] title_delta_t;
		public double[] title_ops_good;
		public double[] title_ops_bad;
		public double[] title_ops_tot;
		public string[] title_name;
		
		public double title_good_tot;
		public double title_good_bad;
		
		public double[] time_capital;
		public double[] time_titles;
		public double[] time_total;
		
		public double time_day_good_tot;
		public double time_day_good_bad;
		public double time_week_good_tot;
		public double time_week_good_bad;
		public double time_month_good_tot;
		public double time_month_good_bad;
		public bool time_print;
		
		public double INDEX;
		
		public int lambda;
		public PositionDB posdb;
		public DB db;
		public Form1 form;

		
        public RunResults(int lambda,PositionDB posdb,DB db,Form1 form)
        {
			this.lambda = lambda;
			this.posdb = posdb;
			this.form = form;
			this.db = db;
			
			accantonamento = taxes = commission = 0;
			
			total_investment = total_gain = delta_t = 0;
			perc_gain = perc_gain_sigma = 0;
			
			ops_tot = posdb.ClosedPositions.Count;
			ops_good = ops_bad = 0;
			ops_very_good = ops_very_bad = 0;
			
			ops_good_gain = ops_bad_gain = 0;
			ops_very_good_gain = ops_very_bad_gain = 0;
			
			int title_num = db.Titles.Length;
			title_name = new string[title_num];
			title_total_investment = new double[title_num];
			title_total_gain = new double[title_num];
			title_delta_t = new double[title_num];
			title_ops_good = new double[title_num];
			title_ops_bad = new double[title_num];
			title_ops_tot = new double[title_num];
			
			title_good_tot = 0;
			title_good_bad = 0;
			
			for(int i = 0;i<title_num;i++)
			{
				title_name[i] = db.Titles[i].Name;
				title_total_investment[i] = 0;
				title_total_gain[i] = 0;
				title_delta_t[i] = 0;
				title_ops_good[i] = 0;
				title_ops_bad[i] = 0;
				title_ops_tot[i] = 0;
			}
			
			time_capital = time_titles = time_total = null;
			
			time_day_good_tot = time_week_good_tot = time_month_good_tot = 0;
			time_day_good_bad = time_week_good_bad = time_month_good_bad = 0;
			time_print = false;
			
			INDEX = 0;
			
			min_commission = 2.95;
            commission_perc = 0.0019;
        }
		
		
        public void Stats()
		{
			double _perc_gain2 = 0;

			for(int i=0;i<ops_tot;i++){
				Position pos = posdb.ClosedPositions.ElementAt(i);
				int q = pos.Quantity;
				double open = pos.Title.Close[pos.TOpen];
				double close = pos.Title.Close[pos.TClose];
				double investment = q*open;
				double raw_gain = q*(close-open);
				
				double _commission = CalculateCommission(investment) + CalculateCommission(q*close);
				double _taxes = Tax(raw_gain);
				double gain = raw_gain - _commission - _taxes;
				
				double _perc_gain = gain/investment;
				
				total_investment += investment;
				total_gain += gain;
				
				taxes += _taxes;
				commission += _commission;
				
				delta_t += pos.Delta_t;
				
				perc_gain += _perc_gain;
				_perc_gain2 += Math.Pow(_perc_gain,2);
				
				if(gain>0) {
					ops_good++;
					ops_good_gain += gain;
				} else {
					ops_bad++;
					ops_bad_gain -= gain;
				}
				
				double threshold = TI.SimulationSigmaALL(pos.Delta_t);
				
				if (_perc_gain > threshold)
				{
					ops_very_good++;
					ops_very_good_gain += gain;
				} else if (_perc_gain < (-1*threshold)){
					ops_very_bad++;
					ops_very_bad_gain -= gain;
				}
			}
			

			if (ops_tot!=0) { 
				delta_t /= ops_tot;
				perc_gain /= ops_tot;
				_perc_gain2 /= ops_tot;
				perc_gain_sigma = Math.Sqrt(_perc_gain2 - Math.Pow(perc_gain,2));
			}
			if (ops_bad != 0) { ops_bad_gain /= ops_bad; }
			if (ops_good != 0) { ops_good_gain /= ops_good; }
			if (ops_very_bad != 0) { ops_very_bad_gain /= ops_very_bad; }
			if (ops_very_good != 0) { ops_very_good_gain /= ops_very_good; }
		}
		
		public void StatsPerTitle()
		{
			int ops_tot = posdb.ClosedPositions.Count;
			int index = 0;
			accantonamento = 0;
			
			for(int i=0;i<ops_tot;i++){
				Position pos = posdb.ClosedPositions.ElementAt(i);
				int q = pos.Quantity;
				double open = pos.Title.Close[pos.TOpen];
				double close = pos.Title.Close[pos.TClose];
				double investment = q*open;
				double raw_gain = q*(close-open);
				
				double _commission = CalculateCommission(investment) + CalculateCommission(q*close);
				double _taxes = Tax(raw_gain);
				double gain = raw_gain - _commission - _taxes;
				
				double _perc_gain = gain/investment;
				
				while(!(pos.Title.Name.Equals(title_name[index])))
				{
					index++;
				}
				
				title_total_investment[index] += investment;
				title_total_gain[index] += gain;
				title_delta_t[index] += pos.Delta_t;
				
				if(gain>0) {
					title_ops_good[index]++;
					title_ops_tot[index]++;
				} else {
					title_ops_bad[index]++;
					title_ops_tot[index]++;
				}
				
			}
			
			int title_num = title_delta_t.Length;
			for(int i=0;i<title_num;i++){
				if(title_ops_tot[i]!=0) title_delta_t[i] /= title_ops_tot[i];
			}
			
			double title_good=0;
			double title_bad=0;
			double title_tot=0;
			
			for(int i=0;i<title_total_gain.Length;i++)
			{
				title_tot++;
				if (title_total_gain[i] > 0) title_good++;
				else if (title_total_gain[i] < 0) title_bad++;
			}
			
			title_good_tot = title_good/title_tot;
			title_good_bad = title_good/title_bad;
		}
		
		DateTime[] datetable;
		int max_time;
		public bool StatsPerTime()
		{
			max_time = 0;
			datetable = null;
			
			for(int i=0;i<db.Titles.Length;i++)
			{
				int len = db.Titles[i].Close.Length;
				if (len > max_time) {
					max_time = len;
					datetable = db.Titles[i].Date.ToArray();
				}
			}
			
			time_capital = new double[max_time];
			time_titles = new double[max_time];
			time_total = new double[max_time];
			
			for(int i=0;i<max_time;i++)
			{
				time_capital[i] = 0;
				time_titles[i] = 0;
				time_total[i] = 0;
			}
			
			// per vedere se tutti i titoli contengono le date presenti nel titolo più lungo
			int trouble = 0;
			for(int i=0;i<db.Titles.Length;i++)
			{
				for(int k=0;k<db.Titles[i].Date.Length;k++)
				{
					bool find = false;
					for(int z=0;z<datetable.Length;z++)
					{
						if(db.Titles[i].Date[k].Equals(datetable[z])) { find = true; }
					}
					if (!find) trouble++;
				}
			}
			if (trouble !=0) return false;

			accantonamento = 0;
			int ops_tot = posdb.ClosedPositions.Count;
			
			for(int i=0;i<ops_tot;i++){
				Position pos = posdb.ClosedPositions.ElementAt(i);
				int q = pos.Quantity;
				double open = pos.Title.Close[pos.TOpen];
				double close = pos.Title.Close[pos.TClose];
				double investment = q*open;
				double raw_gain = q*(close-open);
				
				double _commission = CalculateCommission(investment) + CalculateCommission(q*close);
				double _taxes = Tax(raw_gain);
				double gain = raw_gain - _commission - _taxes;

				// open_date <---> open_index
				DateTime open_date = pos.Title.Date[pos.TOpen];
				int open_index;
				for(open_index=0;open_index<datetable.Length;open_index++)
				{
					if(datetable[open_index].Equals(open_date)) break;
				}
				
				// close_date <---> close_index
				DateTime close_date = pos.Title.Date[pos.TClose];
				int close_index;
				for(close_index=open_index;close_index<datetable.Length;close_index++)
				{
					if(datetable[close_index].Equals(close_date)) break;
				}
				
//				int index = pos.TOpen;
				for(int j = open_index;j < close_index; j++)
				{
					time_capital[j] -= investment;
//					if (pos.Title.Close.Length <= index) System.Console.WriteLine(lambda + " " + pos.Title.Name + " " + open_index + " " + close_index + " " + index + " " + pos.Title.Close.Length);
//					time_titles[j] += q * pos.Title.Close[index];
					time_titles[j] += investment;
//					index++;
				}
				
				
				for(int j = close_index;j<max_time;j++)
					time_capital[j] += gain;
				
			}
			
			int dbcut = 300;
			
			for(int i=dbcut;i<max_time;i++)
				time_total[i] = time_capital[i] + time_titles[i];
			
			double time_day_good = 0;
			double time_day_bad = 0;
			double time_day = 0;
			
			int week_step = 5;
			double time_week = 0;
			double time_week_good = 0;
			double time_week_bad = 0;
			
			int month_step = 22;
			double time_month = 0;
			double time_month_good = 0;
			double time_month_bad = 0;
			
			for(int i=dbcut+1;i<max_time;i++)
			{
				time_day++;
				if (time_capital[i] > time_capital[i-1]) time_day_good++;
				else if (time_capital[i] < time_capital[i-1]) time_day_bad++;
				
				if(i%week_step==0)
				{
				 	time_week++;
					if (time_capital[i] > time_capital[i-week_step]) time_week_good++;
					else if (time_capital[i] < time_capital[i-week_step]) time_week_bad++;
					                                   
				}
					                                   
				if(i%month_step==0)
				{
				 	time_month++;
						
					if (time_capital[i] > time_capital[i-month_step]) time_month_good++;
					else if (time_capital[i] < time_capital[i-month_step]) time_month_bad++;
				}
			}
			
			time_day_good_tot = time_day_good / time_day;
			time_day_good_bad = time_day_good / time_day_bad;
			
			time_week_good_tot = time_week_good / time_week;
			time_week_good_bad = time_week_good / time_week_bad;
			
			time_month_good_tot = time_month_good / time_month;
			time_month_good_bad = time_month_good / time_month_bad;
			

	

			if (time_print)
			{

			}
			
			return true;

		}
		
		
		public void StatsPerTimePrint()
		{
			StreamWriter SW = null;
			try { SW = new StreamWriter(form.simulationfilenameTextbox.Text + ".time"); }
			catch (Exception) { SW = null; }
			
			int dbcut = 300;
			for(int i=dbcut;i<max_time;i++)
				SW.WriteLine((i-dbcut) + " " + datetable[i].Date + " " + time_total[i] + " " + time_capital[i] + " " + time_titles[i]);
			
			SW.Close();
		}
				
        public double min_commission,commission_perc;

        public double CalculateCommission(double amount)
        {
            return (amount < min_commission ? min_commission : amount * commission_perc);
        }

		public double Tax(double amount){
			double tax = amount*0.125;
			if (amount<0) {
				accantonamento -= tax;
				return 0;
			}
			else if (amount>0) {
				if (accantonamento>tax) {
					accantonamento -= tax;
					return 0;
				}
				else if (accantonamento<tax) {
					accantonamento = 0;
					return (tax-accantonamento);
				}
				else if (accantonamento == tax){
					accantonamento = 0;
					return 0;
				}
			}
			return 0;
		}
		
		
		
		public void ComputeIndex()
		{
			double ops_factor = (ops_good/ops_tot) * (ops_good_gain/ops_bad_gain);
			double db_factor = title_good_tot;
			double time_factor = time_week_good_tot;
			
			if (total_gain < 0)
			{
				ops_factor = (ops_bad/ops_tot) * (ops_bad_gain/ops_good_gain);
				db_factor = 1-title_good_tot;
				time_factor = 1-time_week_good_tot;
			}
	
			INDEX = total_gain * ops_factor * time_factor * db_factor;
		}
		

		public void ScatterPlot()
		{
			/*
			StreamWriter SWgood = null;
            try { SWgood = new StreamWriter(form.simulationfilenameTextbox.Text + ".good"); }
			catch (Exception) { SWgood = null; }
			
			StreamWriter SWbad = null;
            try { SWbad = new StreamWriter(form.simulationfilenameTextbox.Text + ".bad"); }
			catch (Exception) { SWbad = null; }
			
			StreamWriter SWneutral = null;
            try { SWneutral = new StreamWriter(form.simulationfilenameTextbox.Text + ".neutral"); }
			catch (Exception) { SWneutral = null; }
			
			StreamWriter SWcut = null;
            try { SWcut = new StreamWriter(form.simulationfilenameTextbox.Text + ".cut"); }
			catch (Exception) { SWcut = null; }
			
			int ops_tot = posdb.ClosedPositions.Count;
			
			double open, close,commission,taxes,gain,investment,perc_gain;
			int q;
			
			int NCUT = 20;
			int MAXVAL = 100;
			int[] cut = new int[NCUT];
			int[] cut_good = new int[NCUT];
			int[] lowcut = new int[NCUT];
			int[] highcut = new int[NCUT];
			int[] lowcut_good = new int[NCUT];
			int[] highcut_good = new int[NCUT];
			for(int i=0;i<NCUT;i++)
			{
				cut[i] = 0;
				cut_good[i] = 0;				
				lowcut[i] = 0;
				highcut[i] = 0;
				lowcut_good[i] = 0;
				highcut_good[i] = 0;
			}

			for(int i=0;i<ops_tot;i++)
			{
				Position pos = posdb.ClosedPositions.ElementAt(i);
				q = pos.Quantity;
				open = db.Titles[pos.ID].Close[pos.TOpen];
				close = db.Titles[pos.ID].Close[pos.TClose];
				investment = q*open;
				commission = CalculateCommission(investment) + CalculateCommission(q*close);
				taxes = Tax(pos.Gain);
				gain = pos.Gain - commission - taxes;
				perc_gain = gain/open;
				
				double ti0 = pos.Title.TI[0][pos.TOpen];
				
				double ti1 = pos.Title.TI[1][pos.TOpen];
				double edge_good = results.perc_gain + results.perc_gain_sigma;
				double edge_bad = results.perc_gain - results.perc_gain_sigma;
				
				if (perc_gain>edge_good)
					SWgood.WriteLine(ti0 + " " + ti1 + " " + perc_gain);
				else if (perc_gain<edge_bad)
					SWbad.WriteLine(ti0 + " " + ti1 + " " + perc_gain);
				else
					SWneutral.WriteLine(ti0 + " " + ti1 + " " + perc_gain);
				
				
				int index = (int) (NCUT * ti0 / MAXVAL);
				cut[index]++;
				if(perc_gain>edge_good)
					cut_good[index]++;
	
			}
			
			SWbad.Close();
			SWgood.Close();
			SWneutral.Close();
			
			
			highcut[0] = cut[0];
			highcut_good[0] = cut_good[0];
			for(int i=1;i<cut.Length;i++)
			{
				highcut[i] = cut[i] + highcut[i-1];
				highcut_good[i] = cut_good[i]+ highcut_good[i-1];
			}
			
			lowcut[cut.Length-1] = cut[cut.Length-1];
			lowcut_good[cut.Length-1] = cut_good[cut.Length-1];
			for(int i=cut.Length-2;i>=0;i--)
			{
				lowcut[i] = cut[i] + lowcut[i+1];
				lowcut_good[i] = cut_good[i]+ lowcut_good[i+1];
			}
			
			for(int i=0;i<highcut.Length;i++)
			{
				SWcut.WriteLine(i*MAXVAL/NCUT + " " + lowcut_good[i] + " " + lowcut[i] + " " + highcut_good[i] + " " + highcut[i]);
			}
		 	SWcut.Close();    
		 	*/                     
		}
    }


    public abstract class TradeSystem
    {
        protected DB db;
        protected PositionDB posdb;
        private Form1 form;
        protected RunResults results;
        public RunResults Results { get { return results; } }

        public TradeSystem(DB db, Form1 form)
        {
            this.db = db;
            this.posdb = new PositionDB();
            this.form = form;
            results = null;
			
			max_investment = 300;
			lambda = 1;
			begin_lambda = 1;
			advanced_statistics = false;
			scatter_plot = false;
		}



        public TradeSystem(TradeSystem tsys)
        {
            this.db = tsys.db;
            this.posdb = tsys.posdb;
            this.form = tsys.form;
			
			max_investment = tsys.max_investment;
			lambda = tsys.lambda;
			begin_lambda = tsys.BeginLambda;
        }
			
			
		private bool advanced_statistics;
		public bool AdvancedStatistics { get { return advanced_statistics; } set { advanced_statistics = value; } }
		
		private bool scatter_plot;
		public bool ScatterPlot { get { return scatter_plot; } set { scatter_plot = value; } }

		private double max_investment;
		public double MaxInvestment { get { return max_investment; } set { max_investment = value; } }
		
		protected int lambda;
		public int Lambda { get { return lambda; } set { lambda = value; } }
		
		protected int begin_lambda;
		public int BeginLambda { get { return begin_lambda; } set { begin_lambda = value; } }

        public void run()
        {
            Thread t = new Thread(do_run);
            t.CurrentCulture = Thread.CurrentThread.CurrentCulture;
            t.CurrentUICulture = Thread.CurrentThread.CurrentUICulture;

            t.Start();
        }


        protected abstract bool SignalBuy(Title t, int i);
        protected abstract bool SignalSell(Title t, Position pos, int i);
		protected abstract void ExtraTIs(Title t);

        protected void do_run()
        {
            for (int k = 0; k < db.Titles.Length; k++)
            {
				ExtraTIs(db.Titles[k]);
				
				for (int i = db.Titles[k].Day0; i < db.Titles[k].Close.Length; i++)
                {
					
					LinkedList<Position> toSell = new LinkedList<Position>();
					
                    // 	SELL <------				
                    foreach (Position pos in posdb.OpenedPositions)
                    {
                        try
                        {
                            if (SignalSell(db.Titles[k], pos,i))
                            {
								// aggiunge a toSell le posizioni da chiudere
								toSell.AddLast(pos);
                                break;
                            }
                        }
                        catch (IndexOutOfRangeException) {
							MessageBox.Show("sell" + k + " time: " + i);
						}// iper debug
                    }
					
					// chiude le posizioni in toSell
					foreach (Position pos in toSell)
						posdb.Close(pos,i);

                    //	BUY <------
                    try
                    {
                        double price = db.Titles[k].Close[i];

                        if (SignalBuy(db.Titles[k], i))
                        {
                            int q = (int)(max_investment / price);
                            if (q > 0)
                            {
                                Position pos = new Position(db.Titles[k], k, q, i);
                                posdb.Open(pos);
                            }
                        }
                    }
                    catch (IndexOutOfRangeException) {
						MessageBox.Show("buy" + k + " time: " + i); 
					}// iper debug
                }
				
				Position[] toSell2 = posdb.OpenedPositions.ToArray();
				for(int i=0;i<toSell2.Length;i++)
				{
					try
					{
						Position pos = toSell2[i];
						posdb.Close(pos, db.Titles[pos.ID].Close.Length - 1);
					}
					catch (IndexOutOfRangeException) { }
				}
            }
			
			
			results = new RunResults(lambda,posdb,db,form);
			
			results.Stats();
			results.StatsPerTime();

			if (advanced_statistics) results.StatsPerTimePrint();
			
			results.StatsPerTitle();
			
			if (scatter_plot) results.ScatterPlot();
			
			results.ComputeIndex();
			

            form.notifySimulationStatus("DONE", this);
       	}


        protected void notify(String s)
        {
            form.notifyStatus("TradeSystemAction", s, 0);
        }
    }


    public class TradeSystemRandom : TradeSystem
    {
        private Random randomizer;

        public TradeSystemRandom(DB db, Form1 form) : base(db, form)
        {
            randomizer = new Random();
            sell_perc = 0.1;
            buy_perc = 0.1;

        }

        private double sell_perc;
        public double Sell_Perc { get { return sell_perc; } set { sell_perc = value; } }

        private double buy_perc;
        public double Buy_Perc { get { return buy_perc; } set { buy_perc = value; } }


		protected override void ExtraTIs(Title t){}
		
        protected override bool SignalBuy(Title t, int i)
        {
            return (randomizer.NextDouble() < buy_perc);
        }

        protected override bool SignalSell(Title t, Position pos, int i)
        {
            return (randomizer.NextDouble() < sell_perc);
        }
    }


    public class TradeSystemAnalytic : TradeSystem
    {
        public TradeSystemAnalytic(DB db, Form1 form)
            : base(db, form)
        {
            sell_perc = 0.1;
            buy_perc = 0.1;
        }

        private double sell_perc;
        public double Sell_Perc { get { return sell_perc; } set { sell_perc = value; } }

        private double buy_perc;
        public double Buy_Perc { get { return buy_perc; } set { buy_perc = value; } }

		protected override void ExtraTIs(Title t){}

        protected override bool SignalBuy(Title t, int i)
        {
			if (i == 0) return false;

            if ( ((t.TI[0][i - 1] <  t.TI[1][i - 1]) && (t.TI[0][i] > t.TI[1][i])) &&
				(t.TI[2][i] > 0) ) return true;
            else
                return false;
        }

        protected override bool SignalSell(Title t, Position pos, int i)
        {
			
			if ((i - pos.TOpen) >= 28) return true;
			else return false;
        }
    }
	
	public class TradeSystemSilly : TradeSystem
    { 	// compra il primo giorno e vende l'ultimo (qua non vende ma venderà comunque durante la run)
        public TradeSystemSilly(DB db, Form1 form) : base(db, form) {}
        protected override bool SignalBuy(Title t, int i) { return (i == t.Day0); }
        protected override bool SignalSell(Title t, Position pos, int i) { return false; }
		protected override void ExtraTIs(Title t){}
    }
	
	public class TradeSystemDelu : TradeSystem
    {
		
        public TradeSystemDelu(DB db, Form1 form)
            : base(db, form)
        {
			randomizer = new Random(DateTime.Now.Millisecond);
            sell_perc = 0.1;
            buy_perc = 0.1;
//			sigma_loss = new double[16];
		}
		
		private Random randomizer;
		
        private double sell_perc;
        public double Sell_Perc { get { return sell_perc; } set { sell_perc = value; } }
		
        private double buy_perc;
        public double Buy_Perc { get { return buy_perc; } set { buy_perc = value; } }
		
        protected override bool SignalBuy(Title t, int i)
        {
			if (i==0) return false;
			int index = (lambda-begin_lambda)+3;
			
			if (t.TI[0][i] < t.TI[1][i]) return false;
			if (t.TI[1][i] < t.TI[2][i]) return false;
/*	
			if (t.TI[0][i] < t.TI[index][i]) return false;
			if (t.TI[1][i] < t.TI[index+1][i]) return false;
			if (t.TI[2][i] < t.TI[index+2][i]) return false;
			
			if (t.TI[index][i] < t.TI[3][i]) return false;
			if (t.TI[index+1][i] < t.TI[4][i]) return false;
			if (t.TI[index+2][i] < t.TI[5][i]) return false;
*/			
			
			return t.TI[0][i-1] < t.TI[index][i-1] && t.TI[0][i] > t.TI[index][i];
			
			return true;
			return (randomizer.NextDouble() < buy_perc);

       }

        protected override bool SignalSell(Title t, Position pos, int i)
        {

			return ((i-pos.TOpen)>=15);
			
        }
		
		double[] sigma_loss;
		
		
		protected override void ExtraTIs(Title t)
		{
			// Tipo per metterci altra roba che non ha la lunghezza dei TI
			// ad esempio il sigma in funzione della lunghezza dell'op per poi fare lo stopp loss
			/*
			int L = sigma_loss.Length;
			sigma_loss[0] = 0;
			for(int i=1;i<L;i++)
				sigma_loss[i] = -((lambda-1)*0.25)*TI.scalar_sigma(TI.calculate_dperc(t.Close,i));
			*/

		}
		
		
    }
}