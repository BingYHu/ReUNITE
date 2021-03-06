﻿using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ReUNITE
{
    public partial class Report : Form
    {
        private BindingSource bindingSource = new BindingSource();

        public Report()
        {
            InitializeComponent();
            List<CsvData> csvdata = ParseCsvFile().ToList();
            comboBox1.DataSource = csvdata.Select(p=>p.MissingFromCity).Distinct().ToList();
            comboBox1.SelectedIndex = 0;

            dateTimePickerFromMissingDate.Value = csvdata.Min(p => p.MmissingFromdate);
            dateTimePickerToMissingDate.Value = csvdata.Max(p => p.MmissingFromdate);

            DataSet nestedData = new DataSet();
            DataTable csvMainDataTable = ToDataTable<CsvData>(csvdata);
            csvMainDataTable.TableName = "Main";
            nestedData.Tables.Add(csvMainDataTable);


            bindingSource.DataSource = nestedData;
            bindingSource.DataMember = "Main";

            ReloadData();
            dataGridView1.DataSource = bindingSource;
        }

        private void ReloadData()
        {
            bindingSource.Filter =
                String.Format("MissingFromCity = '{0}' and (MmissingFromdate >= '{1}' and MmissingFromdate <= '{2}')",
                    comboBox1.SelectedValue, dateTimePickerFromMissingDate.Value, dateTimePickerToMissingDate.Value);

            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            DataView ds = (DataView) bindingSource.List;
        
            IEnumerable<CountByDate> result = from row in ds.ToTable("CountTable").AsEnumerable() 
                         group row by row.Field<DateTime>("MmissingFromdate").ToString("MMM yyyy") into grp
                         select new CountByDate
                         {
                             MmissingFromdateMonth = grp.Key,
                             ChildCount = grp.Count()
                         };

            chart1.Series["Series1"].Points.DataBindXY(result.ToList(), "MmissingFromdateMonth", result.ToList(), "ChildCount");
        }

        public static DataTable ToDataTable<T>(IList<T> data)
        {
            PropertyDescriptorCollection properties =
                TypeDescriptor.GetProperties(typeof(T));
            DataTable table = new DataTable();
            foreach (PropertyDescriptor prop in properties)
                table.Columns.Add(prop.Name, Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType);
            foreach (T item in data)
            {
                DataRow row = table.NewRow();
                foreach (PropertyDescriptor prop in properties)
                    row[prop.Name] = prop.GetValue(item) ?? DBNull.Value;
                table.Rows.Add(row);
            }
            return table;
        }


        public IEnumerable<CsvData> ParseCsvFile()
        {
            using (
                TextFieldParser parser =
                    new TextFieldParser(@"ActiveCases.csv")
            )
            {
                parser.CommentTokens = new string[] { "#" };
                parser.SetDelimiters(new string[] { "," });
 
                // Skip over header line.
                parser.ReadLine();

                while (!parser.EndOfData)
                {
                    string[] fields = parser.ReadFields();
                    yield return new CsvData()
                    {
                        ChildFirstName = fields[1],
                        ChildLastName = fields[2],
                        MissingReportedDate = DateTime.Parse(fields[10]),
                        MmissingFromdate = DateTime.Parse(fields[11]),
                        MissingFromCity = fields[12],
                        MissingFromState = fields[13],
                        MissingFromCountry = fields[14],
                    };
                }

            }

        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
            ReloadData();
        }

        private void Report_Load(object sender, EventArgs e)
        {

        }
    }

    public class CountByDate
    {
        public string MmissingFromdateMonth { get; set; }
        public int ChildCount { get; set; }
    }

    public class CsvData
    {
        public string ChildFirstName { get; set; }
        public string ChildLastName { get; set; }
        public DateTime MissingReportedDate { get; set; }
        public DateTime MmissingFromdate { get; set; }
        public string MissingFromCity { get; set; }
        public string MissingFromState { get; set; }
        public string MissingFromCountry { get; set; }

    }
}
