using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using System.Linq;

namespace WindowsFormsApp1
{
    // note:pls disable ContextSwitchDeadlock exception at debugging

    public partial class Form1 : Form
    {
        private List<Employee> testData = null;
        private const int countTestdata = 10000;
        private readonly string filterString = new Random(DateTime.Now.Millisecond).Next(0, countTestdata).ToString();
        private Dictionary<string, double> results = new Dictionary<string, double>();

        public Form1()
        {
            CreateTestData();
            InitializeComponent();
            this.Load += Form1_Load;
        }

        private void Before()
        {
            BindTestDataToDataGridView();
        }

        private void After()
        {
            BindTestDataToDataGridView();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            RunMonitored(() => FilterChangeVisibilityNotOptimized(), nameof(FilterChangeVisibilityNotOptimized), 4); //mean: 23.337,9000 ms
            RunMonitored(() => FilterChangeVisibilityRemovingRowsInLoop(), nameof(FilterChangeVisibilityRemovingRowsInLoop), 4); //mean: 1.337,2500 ms
            RunMonitored(() => FilterChangeVisibilityClearRowsAddedFiltered(), nameof(FilterChangeVisibilityClearRowsAddedFiltered), 4); //mean: 7,0000 ms
            RunMonitored(() => FilterChangeVisibilityZeroRowHeight(), nameof(FilterChangeVisibilityZeroRowHeight), 4); //mean: 15.109,2500 ms
            RunMonitored(() => FilterChangeVisibilityInvisibleGrid(), nameof(FilterChangeVisibilityInvisibleGrid), 4); //mean: 21.127,0000 ms
            RunMonitored(() => FilterChangeVisibilityFilterWithoutGridView(), nameof(FilterChangeVisibilityFilterWithoutGridView), 4); //mean: 67,2500 ms

            ShowResults();
        }

        private void ShowResults()
        {
            foreach (var item in results.OrderBy(x => x.Value))
            {
                Console.WriteLine($"{item.Key}: {string.Format("{0:N4}", item.Value)}");
            }
        }

        private void FilterChangeVisibilityFilterWithoutGridView()
        {
            var list = new DataGridViewRow[dataGridView1.Rows.Count];
            dataGridView1.Rows.CopyTo(list, 0);

            dataGridView1.Rows.Clear();

            for (int i = 0; i < list.Length; i++)
            {
                object value = list[i].Cells[0].Value;
                if (value != null)
                {
                    list[i].Visible = value.ToString().Contains(filterString);
                }
            }
            dataGridView1.Rows.AddRange(list);
        }
        private void FilterChangeVisibilityInvisibleGrid()
        {
            this.dataGridView1.Visible = false;
            for (int i = 0; i < dataGridView1.Rows.Count; i++)
            {
                object value = dataGridView1.Rows[i].Cells[0].Value;
                if (value != null)
                {
                    dataGridView1.Rows[i].Visible = value.ToString().Contains(filterString);
                }
            }
            this.dataGridView1.Visible = true;
        }
        private void FilterChangeVisibilityNotOptimized()
        {
            for (int i = 0; i < dataGridView1.Rows.Count; i++)
            {
                object value = dataGridView1.Rows[i].Cells[0].Value;
                if (value != null)
                {
                    dataGridView1.Rows[i].Visible = value.ToString().Contains(filterString);
                }
            }
        }
        private void FilterChangeVisibilityRemovingRowsInLoop()
        {
            for (int i = dataGridView1.Rows.Count - 1; i >= 0; i--)
            {
                object value = dataGridView1.Rows[i].Cells[0].Value;
                if (value != null)
                {
                    if (!value.ToString().Contains(filterString))
                        dataGridView1.Rows.RemoveAt(i);
                }
            }
        }
        private void FilterChangeVisibilityZeroRowHeight()
        {
            for (int i = dataGridView1.Rows.Count - 1; i >= 0; i--)
            {
                object value = dataGridView1.Rows[i].Cells[0].Value;
                if (value != null)
                {
                    if (!value.ToString().Contains(filterString))
                        dataGridView1.Rows[i].Height = 0;
                }
            }
        }
        private void FilterChangeVisibilityClearRowsAddedFiltered()
        {
            List<DataGridViewRow> result = new List<DataGridViewRow>();
            for (int i = 0; i < dataGridView1.Rows.Count; i++)
            {
                object value = dataGridView1.Rows[i].Cells[0].Value;
                if (value != null)
                {
                    if (value.ToString().Contains(filterString))
                        result.Add(dataGridView1.Rows[i]);
                }
            }
            dataGridView1.Rows.Clear();
            dataGridView1.Rows.AddRange(result.ToArray());
        }
        private void FilterRemoveRowNotOptimized() { }
        private void BindTestDataToDataGridView()
        {
            dataGridView1.Rows.Clear();
            dataGridView1.Columns.Clear();
            var firstColumn = new DataGridViewTextBoxColumn();
            firstColumn.Name = "MyProperty1";
            dataGridView1.Columns.Add(firstColumn);
            foreach (var item in testData)
            {
                int index = this.dataGridView1.Rows.Add(new DataGridViewRow());
                this.dataGridView1.Rows[index].Cells["MyProperty1"].Value = item.MyProperty1;
            }
        }
        private void Warmup(Action actionToMonitor, string name, int timesToRun = 4)
        {
            Console.WriteLine($"Start warmup runs {name} ({timesToRun})");
            for (int i = 0; i < timesToRun; i++)
            {
                Before();
                Console.WriteLine($"Start warmup run {name}: {i + 1}");
                actionToMonitor();
                Console.WriteLine($"End warmup run {name}: {i + 1}");
                After();
            }
            Console.WriteLine($"End warmup {name}");
        }

        private void RunMonitored(Action actionToMonitor, string name, int timesToRun = 10)
        {
            Warmup(actionToMonitor, name, 3);

            long[] runTime = new long[timesToRun];
            Stopwatch w = new Stopwatch();
            int i = 0;
            Console.WriteLine($"Start benchmarked run {name}");
            while (i < timesToRun)
            {
                Before();
                w.Reset();
                Console.WriteLine($"Start run {name} {i + 1}");
                w.Start();
                actionToMonitor();
                w.Stop();
                Console.WriteLine($"End run {name} {i + 1}");

                Console.WriteLine($"time run: {w.ElapsedMilliseconds}ms");
                After();
                runTime[i] = w.ElapsedMilliseconds;
                i++;
            }
            Console.WriteLine($"End benchmarked run {name}");

            double sum = 0;
            for (int j = 0; j < runTime.Length; j++)
            {
                Console.WriteLine($"Run {j}: {runTime[j]} ms");
                sum += runTime[j];
            }
            var mean = sum / timesToRun;
            results.Add(name, mean);
            Console.WriteLine($"mean: {string.Format("{0:N4}", mean)} ms");
            actionToMonitor(); //run action to show filtered results
        }

        private void CreateTestData()
        {
            this.testData = new List<Employee>(countTestdata);
            for (int i = 0; i < countTestdata; i++)
            {
                this.testData.Add(new Employee
                {
                    MyProperty1 = "sampleString" + i,
                });
            }
        }
    }
    class Employee
    {
        public string MyProperty1 { get; set; }
    }
}
