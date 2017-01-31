using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Globalization;
using System.Threading;

namespace sharpTrade
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Config.init();

            // le righe maggiche
	        CultureInfo myCulture = new CultureInfo("en-US");
            CultureInfo itCulture = new CultureInfo("it-IT");
            myCulture.DateTimeFormat = itCulture.DateTimeFormat;
            Thread.CurrentThread.CurrentCulture = myCulture;
//	        Thread.CurrentThread.CurrentUICulture = myCulture;
            // fino qui
			
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }
}
