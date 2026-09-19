using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using Wpf.Ui;
using Wpf.Ui.Controls;

namespace BcsPosApp
{
    public class UserDto
    {
        public string id { get; set; }
        public string username { get; set; }
        public string role { get; set; }
    }
    
    public class ProductDto
    {
        public string id { get; set; }
        public string name { get; set; }
        public string category { get; set; }
        public double quantity { get; set; }
        public double buy_price { get; set; }
        public double sell_price { get; set; }
    }

    public partial class Dashboard : FluentWindow
    {
        private static readonly HttpClient client = new HttpClient { BaseAddress = new Uri("https://backend.bcs.bcsdeveloper.com/") };
        private readonly SnackbarService _snackbarService;

        static Dashboard()
        {
            client.DefaultRequestHeaders.Add("x-api-key", "backend-bcs-pos");
        }

        public Dashboard()
        {
            InitializeComponent();
            _snackbarService = new SnackbarService();
            _snackbarService.SetSnackbarPresenter(SnackbarPresenter);
            
            // Load data when window loads
            this.Loaded += async (s, e) => await LoadUsersData();
        }

        private async void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            await LoadUsersData();
        }

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            MainWindow login = new MainWindow();
            login.Show();
            this.Close();
        }

        private void BtnNavDashboard_Click(object sender, RoutedEventArgs e)
        {
            DashboardView.Visibility = Visibility.Visible;
            UsersView.Visibility = Visibility.Collapsed;
            ProductsView.Visibility = Visibility.Collapsed;
            
            BtnNavDashboard.Appearance = ControlAppearance.Light;
            BtnNavUsers.Appearance = ControlAppearance.Transparent;
            BtnNavProducts.Appearance = ControlAppearance.Transparent;
            
            lblPageTitle.Text = "Dashboard Overview";
        }

        private void BtnNavUsers_Click(object sender, RoutedEventArgs e)
        {
            DashboardView.Visibility = Visibility.Collapsed;
            UsersView.Visibility = Visibility.Visible;
            ProductsView.Visibility = Visibility.Collapsed;
            
            BtnNavDashboard.Appearance = ControlAppearance.Transparent;
            BtnNavUsers.Appearance = ControlAppearance.Light;
            BtnNavProducts.Appearance = ControlAppearance.Transparent;
            
            lblPageTitle.Text = "System Users";
        }
        
        private void BtnNavProducts_Click(object sender, RoutedEventArgs e)
        {
            DashboardView.Visibility = Visibility.Collapsed;
            UsersView.Visibility = Visibility.Collapsed;
            ProductsView.Visibility = Visibility.Visible;
            
            BtnNavDashboard.Appearance = ControlAppearance.Transparent;
            BtnNavUsers.Appearance = ControlAppearance.Transparent;
            BtnNavProducts.Appearance = ControlAppearance.Light;
            
            lblPageTitle.Text = "Inventory / Products";
        }

        private async Task LoadUsersData()
        {
            try
            {
                LoadingRing.Visibility = Visibility.Visible;

                var usersResponse = await client.GetAsync("api/users");
                var productsResponse = await client.GetAsync("api/data-manager"); // This endpoint returns products

                if (!usersResponse.IsSuccessStatusCode)
                {
                    ShowError("Failed to fetch data from server.");
                    return;
                }

                var usersJsonStr = await usersResponse.Content.ReadAsStringAsync();
                var usersResult = JsonDocument.Parse(usersJsonStr);
                
                // Process Users
                if (usersResult.RootElement.TryGetProperty("success", out var success) && success.GetBoolean())
                {
                    var dataArray = usersResult.RootElement.GetProperty("data");
                    var users = JsonSerializer.Deserialize<List<UserDto>>(dataArray.GetRawText());
                    UsersGrid.ItemsSource = users;

                    if (users != null)
                    {
                        lblTotalUsers.Text = users.Count.ToString();
                        
                        int admins = 0;
                        foreach (var u in users)
                        {
                            if (u.role != null && u.role.ToLower() == "admin")
                                admins++;
                        }
                        lblAdmins.Text = admins.ToString();
                    }
                }
                
                // Process Products
                if (productsResponse.IsSuccessStatusCode)
                {
                    var productsJsonStr = await productsResponse.Content.ReadAsStringAsync();
                    var productsResult = JsonDocument.Parse(productsJsonStr);
                    if (productsResult.RootElement.TryGetProperty("success", out var prodSuccess) && prodSuccess.GetBoolean())
                    {
                        var prodArray = productsResult.RootElement.GetProperty("data");
                        var products = JsonSerializer.Deserialize<List<ProductDto>>(prodArray.GetRawText());
                        
                        ProductsGrid.ItemsSource = products;
                        
                        if (products != null)
                        {
                            lblProducts.Text = products.Count.ToString();
                        }
                    }
                }
                
                ShowSuccess("Data refreshed successfully!");
            }
            catch (Exception ex)
            {
                ShowError("Connection error: " + ex.Message);
            }
            finally
            {
                LoadingRing.Visibility = Visibility.Collapsed;
            }
        }

        private void ShowError(string message)
        {
            _snackbarService.Show(
                "Error", 
                message, 
                ControlAppearance.Danger, 
                new SymbolIcon(SymbolRegular.ErrorCircle24), 
                TimeSpan.FromSeconds(4)
            );
        }

        private void ShowSuccess(string message)
        {
            _snackbarService.Show(
                "Success", 
                message, 
                ControlAppearance.Success, 
                new SymbolIcon(SymbolRegular.CheckmarkCircle24), 
                TimeSpan.FromSeconds(3)
            );
        }
    }
}
