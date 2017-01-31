using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.Text;
using System.Windows.Forms;



namespace sharpTrade
{
    class TIfacade
    {
        public static void init()
        { TI.init(); }


        public static bool evaluate_expr(String expression, DBTrade db)
        {
            bool ret = true;

            String[] subexprs = expression.Split(';');

            foreach (String subexp in subexprs)
            {
                if (subexp.Equals("") || subexp.StartsWith("//"))
                    continue;

                ret &= evaluate_subexpr(subexp, db);
                if (!ret)
                    return false;
            }

            return true;
        }


        private static bool evaluate_subexpr(String expression, DBTrade db)
        {
            String lefttitle = "";
            String leftfield = "";
            String righttitle = "";
            String rightfield = "";
            String command = "";
            double[] parms = new double[10];
            NumberFormatInfo ni = (NumberFormatInfo)CultureInfo.InstalledUICulture.NumberFormat.Clone();
            ni.NumberDecimalSeparator = ".";

            expression = expression.Trim();

            if (expression.Contains('='))
            {
                leftfield = expression.Substring(0, expression.IndexOf('=')).Trim();

                // per farlo bene, x.(__) x è un nome di titolo?
                if (leftfield.Contains('.'))
                {
                    lefttitle = leftfield.Substring(0, leftfield.IndexOf('.'));
                    leftfield = leftfield.Substring(leftfield.IndexOf('.')+1);
                }
                expression = expression.Substring(expression.IndexOf('=') + 1).Trim();
            }
            // lefttitle e leftfield apposto.

            expression = expression.Replace(")", "");
            if (!expression.Contains('('))
                return false;
            command = expression.Substring(0, expression.IndexOf('('));
            expression = expression.Substring(expression.IndexOf('(')+1);


            String[] otherparms = expression.Trim().Split(',');
            if (!otherparms[0].Contains('.'))
                return false;

            righttitle = otherparms[0].Substring(0, otherparms[0].IndexOf('.'));
            rightfield = otherparms[0].Substring(otherparms[0].IndexOf('.')+1);


            for (int j = 1; j < otherparms.Length; j++)
            {
                try { parms[j - 1] = Double.Parse(otherparms[j], ni); }
                catch (FormatException) { return false; }
            }

            if (lefttitle.Equals(""))       // if not specified left title
                lefttitle = righttitle;

            // now left* right*, command, parms are ok
//            MessageBox.Show(lefttitle + "." + leftfield + " = "+command+"(" + righttitle + "." + rightfield+")");
            DBTitle srcTitle = db.getByName(righttitle);
            DBTitle dstTitle = db.getByName(lefttitle);
            if (srcTitle == null || dstTitle == null)
                return false;

            double[] srcSerie = srcTitle.GetSerieByName(rightfield);
            if (srcSerie == null)
                return false;



            double[] result = null;
            if (command.Equals("const"))
                result = TI.calculate_const(srcSerie.Length, parms[0]);
            else if (command.Equals("mm"))
                result = TI.calculate_mm(srcSerie, (int)parms[0]);
            else if (command.Equals("emm"))
                result = TI.calculate_emm(srcSerie, (int)parms[0], parms[1]);

            else if (command.Equals("d") || command.Equals("d-"))
                result = TI.calculate_d(srcSerie, (int)parms[0]);
            else if (command.Equals("d%") || command.Equals("d-%") || command.Equals("dperc") || command.Equals("d-perc"))
                result = TI.calculate_dperc(srcSerie, (int)parms[0]);
            else if (command.Equals("d2") || command.Equals("d2-"))
                result = TI.calculate_d2(srcSerie, (int)parms[0]);

            else if (command.Equals("sigma"))
                result = TI.calculate_sigma(srcSerie, (int)parms[0]);
            else if (command.Equals("esigma"))
                result = TI.calculate_esigma(srcSerie, (int)parms[0], parms[1]);

            else if (command.Equals("BBhigh"))
                result = TI.calculate_BBhigh(srcSerie, (int)parms[0], parms[1]);
            else if (command.Equals("BBlow"))
                result = TI.calculate_BBlow(srcSerie, (int)parms[0], parms[1]);
            else if (command.Equals("BBWp"))
                result = TI.calculate_BBWp(srcSerie, (int)parms[0], parms[1]);
            else if (command.Equals("BBBp"))
                result = TI.calculate_BBBp(srcSerie, (int)parms[0], parms[1]);

            else if (command.Equals("range"))
                result = TI.calculate_range(srcSerie, (int)parms[0]);
            else if (command.Equals("stochK"))
                result = TI.calculate_stochasticK(srcSerie, (int)parms[0], parms[1]);

            else if (command.Equals("RSIC"))
                result = TI.calculate_RSIC(srcSerie, (int)parms[0]);
            else if (command.Equals("RSIV"))
                result = TI.calculate_RSIV(srcSerie, (int)parms[0]);



            else if (command.Equals("rand"))
                result = TI.calculate_rand(srcSerie, parms[0], parms[1]);



            if (result != null)
            {
                dstTitle.AddSerie(leftfield, result);
                return true;
            }
            else
                return false;
        }
    }
}
