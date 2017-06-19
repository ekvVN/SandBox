namespace Benchmarks
{
    using System.Windows.Forms;
    using BenchmarkDotNet.Running;

    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            
            BenchmarkRunner.Run<GpuAlea>();
        }
    }
}
