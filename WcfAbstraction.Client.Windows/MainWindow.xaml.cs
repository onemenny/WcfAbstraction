using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WcfAbstraction.Server.Proxies;
using WcfAbstraction.Server.Services;
using WcfAbstraction.Server.Services.Proxies;

namespace WcfAbstraction.Client.Windows
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

#pragma warning disable 168
            UnityRegistry unityRegistry = UnityRegistry.Instance; //make sure we load our settings
#pragma warning restore 168
        }

        #region Examples For Creating Services

        private Proxy<ITestService> ManualCreateTestServiceProxy()
        {
            //TODO menny: document
            
            //get DefaultNetTcpBinding from the app.config 
            //the app.config binding must be identical to our server bindings 
            var netTcpBindings = new NetTcpBinding("DefaultNetTcpBinding");

            return Proxy<ITestService>
                .Create(netTcpBindings, new EndpointAddress("net.tcp://localhost:8000/TestService"));
        }

        private Proxy<ITestService> DefaultWcfConfigurationTestServiceProxy()
        {
            //TODO menny: document
            return Proxy<ITestService>.Create();
        }

        private ITestService DefaultUnityContainerTestServiceProxy()
        {
            //TODO menny: document

            return UnityRegistry.GetService<ITestService>();
        }

        #endregion

        private void TestAlive_Click(object sender, RoutedEventArgs e)
        {
            TestAliveResponse.Text = string.Empty;

            bool res;
            using (var proxy = Proxy<ITestService>.Create())
            {
                res = proxy.Channel.IsAlive();
            }

            TestAliveResponse.Text = res.ToString();
        }

        private void TestException_Click(object sender, RoutedEventArgs e)
        {
            TestExceptionResponse.Text = string.Empty;

            try
            {
                UnityRegistry.GetService<ITestService>().AssertArgumentNotNull_GenericFault(null);
            }
            catch (FaultException<ArgumentNullException> ex)
            {
                //will occure
                Console.WriteLine(ex);
            }
            catch (FaultException ex)
            {
                //will not occure
                Console.WriteLine(ex);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            TestExceptionResponse.Text = string.Empty;
        }

        private void TestException2_Click(object sender, RoutedEventArgs e)
        {
            TestExceptionResponse.Text = string.Empty;

            try
            {
                UnityRegistry.GetService<ITestService>().AssertArgumentNotNull_DefaultFault(null);
            }
            catch (FaultException<ArgumentNullException> ex)
            {
                //will not occure
                Console.WriteLine(ex);
            }
            catch (FaultException ex)
            {
                //will occure
                Console.WriteLine(ex);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            TestExceptionResponse.Text = string.Empty;
        }

        private void TestEntity_Click(object sender, RoutedEventArgs e)
        {
            var ent = UnityRegistry.GetService<ITestService>().GetNewTestEntity();
            TestEnityResponse.Text = ent.ToString();
        }

        private void TestCircularRef_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var ent = UnityRegistry.GetService<ITestService>().GetCircularRefEntities();
                TestEnityResponse.Text = ent.Children.Count.ToString(CultureInfo.InvariantCulture);
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }
        

    }
}
