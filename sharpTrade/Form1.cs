using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;




namespace sharpTrade
{
    public partial class Form1 : Form
    {
        private const String CSV_Dirname = "./files_csv/";

        private DB db;
        private String current_filename;
        private TaskList tasklist;


        public Form1()
        {
            current_filename = "";
            GenericActionUrl = "http://ichart.yahoo.com/table.csv?s=$NAME&a=00&b=1&c=2003&d=$MONTH&e=$DAY&f=$YEAR&g=d&ignore=.csv";

            tasklist = new TaskList(this);
            db = new DB();

            InitializeComponent();

            TradesystemTypeComboBox.SelectedIndex = 0;
            db.ImportCSVDir(CSV_Dirname);
            HeartBeat();
            FindSimulationFilename();
        }



        // this function should update ALL the menus and dynamic contents
        private void HeartBeat()
        {
            comboBox1.Items.Clear();

            titleGridView.Rows.Clear();
            valuesGridView.Rows.Clear();

            textBox3.Text = "" + db.Titles.Length;

            foreach (Title t in db.Titles)
            {
                comboBox1.Items.Add(t.Name);
                titleGridView.Rows.Add(new object[] { t.Name, t.Tstart, t.Tend /*, t.Close[0] , t.Close[t.Close.Length-1] */});
            }
        }

        private void importcsvToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                db.ImportCSV(dialog.FileName);
                HeartBeat();
            }

            dialog.Dispose();
        }


        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox1.SelectedIndex < 0)
                return;

            Title title = db.Titles[comboBox1.SelectedIndex];
            if (title == null)
                return;
            
            valuesGridView.Rows.Clear();
            int maxseriesize = title.Length;

            valuesGridView.ColumnCount = 7 + title.TI.Length;

            int column_index = 0;
            valuesGridView.Columns[column_index++].Name = "Date";
            valuesGridView.Columns[column_index++].Name = "Open";
            valuesGridView.Columns[column_index++].Name = "High";
            valuesGridView.Columns[column_index++].Name = "Low";
            valuesGridView.Columns[column_index++].Name = "Close";
            valuesGridView.Columns[column_index++].Name = "Volume";
            valuesGridView.Columns[column_index++].Name = "Adj_close";

            for (int i=0; i< title.TI.Length; i++)
                valuesGridView.Columns[column_index++].Name = "TI_"+title.TI_Name[i];

            for (int i = 0; i < title.Open.Length; i++)
            {
                object[] o = new object[column_index];

                int serieindex = 0;

                o[serieindex++] = title.Date[i];
                o[serieindex++] = title.Open[i];
                o[serieindex++] = title.High[i];
                o[serieindex++] = title.Low[i];
                o[serieindex++] = title.Close[i];
                o[serieindex++] = title.Volume[i];
                o[serieindex++] = title.AdjClose[i];

                for (int k = 0; k < title.TI.Length; k++)
                    o[serieindex++] = title.TI[k][i];

                valuesGridView.Rows.Add(o);
            }
        }



        private void importCsvDirToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            dialog.SelectedPath = Config.Instance.IncomingDirectory;

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                db.ImportCSVDir(dialog.SelectedPath);
                HeartBeat();
            }
            dialog.Dispose();
        }


        delegate void DelegateNotifyStatus(String msg, String url, double perc);
        public void notifyStatus(String msg, String url, double perc)
        {
            if (TradeSystemActionsTextbox.InvokeRequired)
            {
                try
                {
                    DelegateNotifyStatus method = notifyStatus;
                    this.Invoke(method, new object[] { msg, url, perc });
                }
                catch (ObjectDisposedException) { }
            }
            else
            {
                if (msg.Equals("TradeSystemAction"))
                {
                    TradeSystemActionsTextbox.AppendText(url+"\n");
                }
                else if (msg.Equals("TradeSystemDBPurge"))
                {
                    HeartBeat();
                }
            }
        }





        public void NotifyNewFile(String filename)
        { }





        private String GenericActionUrl;
        private void updateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            update();
        }


        private void update()
        {
            try
            {
                StreamReader SR = new StreamReader("csv_list.txt");
                GenericActionUrl = "";

                do { GenericActionUrl = SR.ReadLine().Trim(); }
                while (GenericActionUrl.StartsWith("//"));


                String actionname;

                while ((actionname = SR.ReadLine()) != null)
                {
                    actionname = actionname.Trim();
                    if (actionname.StartsWith("//"))
                        continue;

                    titleGridView.Rows.Add(new object[] { actionname, "unknown", "unknown" });
                }
                //                MessageBox.Show(GenericActionUrl);

                SR.Close();
            }
            catch (IOException) { }
        }



        private void upgradeToolStripMenuItem_Click(object sender, EventArgs e)
        { upgrade(); }

        
        private void upgrade()
        {
            if (GenericActionUrl.Length == 0)
            {
                MessageBox.Show("Update first.");
                return;
            }

            DateTime today = DateTime.Today;
            tasklist.Abort();

            for (int i = 0; i < titleGridView.Rows.Count; i++)
                if (titleGridView.Rows[i].Cells[1].Value.Equals("unknown") && titleGridView.Rows[i].Cells[2].Value.Equals("unknown"))
                {
                    String action_url = GenericActionUrl.Replace("$NAME", titleGridView.Rows[i].Cells[0].Value.ToString());
                    action_url = action_url.Replace("$DAY", today.Day.ToString());
                    action_url = action_url.Replace("$MONTH", (today.Month - 1).ToString());
                    action_url = action_url.Replace("$YEAR", today.Year.ToString());

                    //                  MessageBox.Show(action_url);
                    tasklist.newTaskFile(action_url, Config.Instance.IncomingDirectory + titleGridView.Rows[i].Cells[0].Value.ToString() + ".csv", 0);
                }

            tasklist.Start();
        }


        private void upgradeSelectedToolStripMenuItem_Click(object sender, EventArgs e)
        { upgrade_selected(); }

        private void upgrade_selected()
        {
            if (GenericActionUrl.Length == 0)
            {
                MessageBox.Show("Update first.");
                return;
            }

            if (titleGridView.SelectedCells.Count == 0)
            {
                MessageBox.Show("And you selected what, for fuck sake?!");
                return;
            }

            DateTime today = DateTime.Today;
            tasklist.Abort();

            for (int i = 0; i < titleGridView.SelectedCells.Count; i++)
            {
                String action_name = titleGridView.Rows[titleGridView.SelectedCells[i].RowIndex].Cells[0].Value.ToString();

                String action_url = GenericActionUrl.Replace("$NAME", action_name);
                action_url = action_url.Replace("$DAY", today.Day.ToString());
                action_url = action_url.Replace("$MONTH", (today.Month - 1).ToString());
                action_url = action_url.Replace("$YEAR", today.Year.ToString());

//                MessageBox.Show("i would upgrade: " + action_url);
                tasklist.newTaskFile(action_url, Config.Instance.IncomingDirectory + action_name + ".csv", 0);
            }

            tasklist.Start();
        }

        private void TradeSystemActionsSave_Click(object sender, EventArgs e)
        {
            if (TradeSystemActionsTextbox.Text.Length == 0)
            {
                MessageBox.Show("There is nothing to save.");
                return;
            }

            SaveFileDialog dialog = new SaveFileDialog();
            dialog.DefaultExt = "txt";
            dialog.Filter = "Text File (*.txt)|*.txt|All files (*.*)|*.*";
            dialog.AddExtension = true;

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                String filename = dialog.FileName;

                if (dialog.FilterIndex == 1)
                {            // save .txt
                    if (!filename.EndsWith("txt"))
                        filename = filename + ".txt";
                }

                try
                {
                    StreamWriter SW = new StreamWriter(filename);
                    SW.Write(TradeSystemActionsTextbox.Text);
                    SW.Close();
                }
                catch (IOException) { MessageBox.Show("Error saving TradeSystemActionFile: " + filename); }
            }

            dialog.Dispose();
        }
		
		private void dropToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Are you sure?", "DROP Entire DB...", MessageBoxButtons.YesNo, MessageBoxIcon.Question)
                == DialogResult.Yes)
            {
                db = new DB();
                HeartBeat();
            }
        }
		
		delegate void DelegateSimulationStatus(String msg, TradeSystem t);
        public void notifySimulationStatus(String msg, TradeSystem t)
        {
            if (TradeSystemActionsTextbox.InvokeRequired)
            {
                try
                {
                    DelegateSimulationStatus method = notifySimulationStatus;
                    this.Invoke(method, new object[] { msg, t });
                }
                catch (ObjectDisposedException) { }
            }
            else
            {
                if (msg.Equals("DONE"))
                {
                    sim_results[sim_done++] = t.Results;

                    RunN = Int32.Parse(textBox1.Text);
					TotSim = (int) ((MaxLambda-InitLambda)*RunN);
					
					int val0 = (int) (100*sim_done/TotSim);
					int val = val0 - progressBar1.Value;
					progressBar1.Increment(val);
					
					TradeSystemActionsTextbox.Clear();
					double timeremaining = (TotSim-sim_done)*((DateTime.Now - time_start).TotalSeconds)/(sim_done);
					int min = (int) (timeremaining/60);
					int sec = (int) (timeremaining%60);
					if (min == 0)
						TradeSystemActionsTextbox.AppendText("Time Remaining: " + sec + "''\n");
					else
						TradeSystemActionsTextbox.AppendText("Time Remaining: " + min + "' " + sec + "''\n");
					
					if (sim_done==1)
					{
						TradeSystemActionsTextbox.Clear();
						TradeSystemActionsTextbox.AppendText("Time Remaining: -----\n");
					}

					
                    if (sim_done == TotSim)
                    {
						TradeSystemActionsTextbox.Clear();
						sec = (int)((DateTime.Now - time_start).TotalSeconds);
						min = (int) (sec/60);
						sec = (int) (sec%60);
						if (min == 0)
							TradeSystemActionsTextbox.AppendText("Simulation Time: " + sec + "''\n");
						else
							TradeSystemActionsTextbox.AppendText("Simulation Time: " + min + "' " + sec + "''\n");
						
						
                        tradesystem_tasklist = null;
						
						ManageResults();

                        FindSimulationFilename();
                        maxlambdaTextbox.ReadOnly = false;
                        initlambdaTextbox.ReadOnly = false;
                        textBox1.ReadOnly = false;
//						SomeStuff();
                    }
                    else
                    {
                        int index = sim_done + max_threads - 1;

                        if (index < tradesystem_tasklist.Count)
                            tradesystem_tasklist.ElementAt(index).run();
                    }
                }
            }
			
        }
		
		int RunN;
		int TotSim;
		
		private void ManageResults()
		{
	        double[] ops_good = new double[MaxLambda];
			double[] ops_good_gain = new double[MaxLambda];
			
			double[] ops_bad = new double[MaxLambda];
	        double[] ops_bad_gain = new double[MaxLambda];
			
			double[] ops_very_good = new double[MaxLambda];
			double[] ops_very_bad = new double[MaxLambda];
			
	        double[] ops_tot = new double[MaxLambda];
	        double[] delta_t = new double[MaxLambda];
	        double[] total_gain = new double[MaxLambda];
			double[] total_investment = new double[MaxLambda];
			
			double[] title_good_tot = new double[MaxLambda];
			double[] title_good_bad = new double[MaxLambda];
			
			double[] time_day_good_tot = new double[MaxLambda];
			double[] time_day_good_bad = new double[MaxLambda];
			double[] time_week_good_tot = new double[MaxLambda];
			double[] time_week_good_bad = new double[MaxLambda];
			double[] time_month_good_tot = new double[MaxLambda];
			double[] time_month_good_bad = new double[MaxLambda];
			
			double[] INDEX = new double[MaxLambda];

	        for (int i = 0; i < MaxLambda; i++)
	        {
	            ops_good[i] = 0;
				ops_good_gain[i] = 0;
				
	            ops_bad[i] = 0;
				ops_bad_gain[i] = 0;
				
	            ops_very_good[i] = 0;
				ops_very_bad[i] = 0;
				
	            ops_tot[i] = 0;
				delta_t[i] = 0;
	            total_gain[i] = 0;
				total_investment[i] = 0;
				
				title_good_tot[i] = 0;
				title_good_bad[i] = 0;
				
				time_day_good_tot[i] = 0;
				time_day_good_bad[i] = 0;
				time_week_good_tot[i] = 0;
				time_week_good_bad[i] = 0;
				time_month_good_tot[i] = 0;
				time_month_good_bad[i] = 0;
				
				INDEX[i] = 0;
	        }
	
	        for (int i = 0; i < TotSim; i++)
	        {
	            int index = sim_results[i].lambda;
	
	            ops_good[index] += sim_results[i].ops_good;
				ops_good_gain[index] += sim_results[i].ops_good_gain;
				
	            ops_bad[index] += sim_results[i].ops_bad;
				ops_bad_gain[index] += sim_results[i].ops_bad_gain;
				
	            ops_very_good[index] += sim_results[i].ops_very_good;
				ops_very_bad[index] += sim_results[i].ops_very_bad;
				
				ops_tot[index] += sim_results[i].ops_tot;
	            delta_t[index] += sim_results[i].delta_t;
	            total_gain[index] += sim_results[i].total_gain;
				total_investment[index] += sim_results[i].total_investment;
				
				title_good_tot[index] += sim_results[i].title_good_tot;
				title_good_bad[index] += sim_results[i].title_good_bad;
				
				time_day_good_tot[index] += sim_results[i].time_day_good_tot;
				time_day_good_bad[index] += sim_results[i].time_day_good_bad;
				time_week_good_tot[index] += sim_results[i].time_week_good_tot;
				time_week_good_bad[index] += sim_results[i].time_week_good_bad;
				time_month_good_tot[index] += sim_results[i].time_month_good_tot;
				time_month_good_bad[index] += sim_results[i].time_month_good_bad;
				
				INDEX[index] += sim_results[i].INDEX;
	        }
			
	        for (int i = InitLambda; i < MaxLambda; i++)
	        {
	            ops_good[i] /= RunN;
				ops_good_gain[i] /= RunN;
				
	            ops_bad[i] /= RunN;
				ops_bad_gain[i] /= RunN;
				
				ops_very_good[i] /= RunN;
	            ops_very_bad[i] /= RunN;
											
	            ops_tot[i] /= RunN;
				delta_t[i] /= RunN;
	            total_gain[i] /= RunN;
				total_investment[i] /= RunN;
				
				title_good_tot[i] /= RunN;
				title_good_bad[i] /= RunN;
				
				time_day_good_tot[i] /= RunN;
				time_day_good_bad[i] /= RunN;
				time_week_good_tot[i] /= RunN;
				time_week_good_bad[i] /= RunN;
				time_month_good_tot[i] /= RunN;
				time_month_good_bad[i] /= RunN;
				
				INDEX[i] /= RunN;
	        }
	
	        StreamWriter SW = null;
	        try { SW = new StreamWriter(simulationfilenameTextbox.Text); }
	        catch (Exception) { SW = null; }
	
	
	        for (int i = InitLambda; i < MaxLambda; i++)
	        {
				TradeSystemActionsTextbox.AppendText("[" + i + "] PROFIT: " + ((int)(total_gain[i])) + "\n");
				
				SW.Write(i + " "); // 1
				SW.Write(total_gain[i] + " " + total_investment[i] + " "); // 2 3
				SW.Write(ops_good[i] + " " + ops_bad[i] + " " +  ops_tot[i] + " "); // 4 5 6
				SW.Write(ops_good_gain[i] + " " + ops_bad_gain[i] + " "); // 7 8
				SW.Write(ops_very_good[i] + " " + ops_very_bad[i] + " "); // 9 10
				SW.Write(title_good_tot[i] + " " + time_day_good_bad[i] + " "); // 11 12
				SW.Write(time_day_good_tot[i] + " " + time_day_good_bad[i] + " "); // 13 14
				SW.Write(time_week_good_tot[i] + " " + time_week_good_bad[i] + " "); // 15 16
				SW.Write(time_month_good_tot[i] + " " + time_month_good_bad[i] + " "); // 17 18
				SW.Write(delta_t[i] + " "); // 19
				SW.Write(INDEX[i] + " "); // 20
				SW.WriteLine();
	    	}
			
			SW.Close();
			
		}
						       
						       
		

		
		
        int sim_done;
        protected RunResults[] sim_results;
		int MaxLambda,InitLambda;
        private LinkedList<TradeSystem> tradesystem_tasklist = null;
        public const int max_threads = 2;

		DateTime time_start;
        private bool first_run = true;
        private void TradeSystemActionsRun_Click(object sender, EventArgs e)
        {
			TradeSystemActionsTextbox.Clear();
			sim_results = null;
			sim_done = 0;
			
			
            if (!first_run)
            {
                first_run = false;
                db.Clear();
                db.ImportCSVDir(CSV_Dirname);
            }

			int RunN;
            try { RunN = Int32.Parse(textBox1.Text); }
            catch (Exception) { RunN = 1; }

            try { InitLambda = Int32.Parse(initlambdaTextbox.Text); }
            catch (Exception) { InitLambda = 1; }

            try { MaxLambda = Int32.Parse(maxlambdaTextbox.Text); }
            catch (Exception) { MaxLambda = InitLambda+1; }

            if (MaxLambda <= InitLambda)
                MaxLambda = InitLambda + 1;

            sim_done = 0;
            textBox1.ReadOnly = true;
            sim_results = new RunResults[RunN*MaxLambda];
            maxlambdaTextbox.ReadOnly = true;
            initlambdaTextbox.ReadOnly = true;


            if (tradesystem_tasklist != null)
            {
                MessageBox.Show("Treads still running");
                return;
            }
            tradesystem_tasklist = new LinkedList<TradeSystem>();

			for(int k = InitLambda;k<MaxLambda;k++)
			{
	            for (int i = 0; i < RunN; i++)
	            {
	                TradeSystem tss = null;
	                if (TradesystemTypeComboBox.SelectedItem.ToString().Equals("Random"))
					{
						if((k==InitLambda)&&(i==0)) InitDB();
	                    tss = new TradeSystemRandom(db, this);

					}
	                else if (TradesystemTypeComboBox.SelectedItem.ToString().Equals("Analytic"))
	                {
						if((k==InitLambda)&&(i==0)) InitAnalytic();
						
	                    tss = new TradeSystemAnalytic(db, this);
						
	                }
					else if (TradesystemTypeComboBox.SelectedItem.ToString().Equals("Delu"))
	                {
						if((k==InitLambda)&&(i==0)) InitDelu();
						tss = new TradeSystemDelu(db, this);
						System.Threading.Thread.Sleep(1);
	                }
					else if (TradesystemTypeComboBox.SelectedItem.ToString().Equals("Silly"))
	                {
						if((k==InitLambda)&&(i==0)) InitDB();
						tss = new TradeSystemSilly(db, this);
	                }
					
					else if (TradesystemTypeComboBox.SelectedItem.ToString().Equals("Neural"))
	                {
						double[] prova = new double[100];
						for(int zz=0;zz<prova.Length;zz++)
							prova[zz] = zz;
						
						double[] prova1 = TI.calculate_mm(prova,30);
						double[] prova11 = TI.calculate_mm_base(prova,30,1);
						double[] prova2 = TI.calculate_mm_base_new(prova,30,1);
						double[] prova3 = TI.calculate_emm(prova,30,0.5);
						double[] prova33 = TI.calculate_mm_base(prova,30,2);
						double[] prova4 = TI.calculate_mm_base_new(prova,30,2);
						
						System.Console.WriteLine(prova1[99] + " " + prova11[99] + " " + prova2[99]);
						System.Console.WriteLine(prova3[99] + " " + prova33[99] + " " + prova4[99]);
						
						/*
						StreamWriter SW = null;
				        try { SW = new StreamWriter("test"); }
				        catch (Exception) { SW = null; }
						
				
						for(int z=0;z<vals.Length;z++)
							SW.WriteLine(vals[z] + " " + oldmean[z] + " " + newmean[z]);
						*/
						if((k==InitLambda)&&(i==0)) InitDB();
	                    tss = new TradeSystemRandom(db, this);
						
	                }
			
	                else
	                    continue;
	
//	                try { tss.InitialCapital = Double.Parse(textBoxInitialCapital.Text); }
//	                catch (Exception) { tss.InitialCapital = 0; }
	
	                try { tss.MaxInvestment = Double.Parse(textBox2.Text); }
	                catch (Exception) { tss.MaxInvestment = 0; }

                    tss.AdvancedStatistics = checkBox2.Checked;
                    tss.ScatterPlot = checkBox1.Checked;
					tss.BeginLambda = InitLambda;
					tss.Lambda = k;
					
                    tradesystem_tasklist.AddLast(tss);
	            }
			}
			
			time_start = DateTime.Now;
			int thread_num = Math.Min(max_threads, tradesystem_tasklist.Count);
            for (int i = 0; i < thread_num; i++)
                tradesystem_tasklist.ElementAt(i).run();

        }

        public void FindSimulationFilename()
        {
            DirectoryInfo rootdir = new DirectoryInfo("./results/");
            if (!rootdir.Exists)
                rootdir.Create();

            int i = 0;
            while ((new FileInfo("./results/sim"+i)).Exists)
                i++;

            simulationfilenameTextbox.Text = "./results/sim"+i;
        }


		private void InitDelu()
		{
			
			foreach (Title t in db.Titles)
			{
				
				t.AddTI("mm", TI.calculate_mm_base_new(t.Close,2,1));
				t.AddTI("mm", TI.calculate_mm_base_new(t.Close,67,1));
				t.AddTI("mm", TI.calculate_mm_base_new(t.Close,206,1));
				/*
				t.AddTI("mm", TI.calculate_mm_base_new(t.Close,2,1));
				t.AddTI("mm",TI.calculate_mm_base_new(t.High,2,1));
				t.AddTI("mm",TI.calculate_mm_base_new(t.Low,2,1));
				
				t.AddTI("mm", TI.calculate_mm_base_new(t.Close,299	,1));
				t.AddTI("mm",TI.calculate_mm_base_new(t.High,299,1));
				t.AddTI("mm",TI.calculate_mm_base_new(t.Low,299,1));
				*/
				for(int q = InitLambda;q<MaxLambda;q++){
					t.AddTI("mm",TI.calculate_mm_base_new(t.Close,q,1));
				}

			}
			
			InitDB();
		}
		
		private void InitAnalytic()
		{
			foreach (Title t in db.Titles)
			{
				t.AddTI("mm0", TI.calculate_mm(t.Close,1));
				t.AddTI("mm6", TI.calculate_mm(t.Close,6));
				t.AddTI("d109", TI.calculate_d(t.Close,109));
			}
			InitDB();

		}
		
		DateTime t0,t1;

		private void InitDB()
		{
			t0 = dateTimePicker1.Value;
            t1 = dateTimePicker2.Value;

            db.Purge(t0, t1, 300);		// Messo a 300 per avere sempre lo stesso DB, non si faranno mai TI più lunghi di 200
			notifyStatus("TradeSystemDBPurge", "", 0);
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox2.Checked == true)
                maxlambdaTextbox.Text = "2";
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox1.Checked == true)
                maxlambdaTextbox.Text = "2";
        }
		
		private void SomeStuff()
		{
			StreamWriter SW = null;
            try { SW = new StreamWriter("stuff"); }
            catch (Exception) { SW = null; }
			
			int TotSim = MaxLambda-InitLambda;
			for(int i=0;i<TotSim;i++){
				SW.WriteLine(sim_results[i].lambda + " " + sim_results[i].perc_gain + " " + sim_results[i].perc_gain_sigma);
			}
			SW.Close();
		}
    }
}