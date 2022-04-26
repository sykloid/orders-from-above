using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Modules;
using Blish_HUD.Modules.Managers;
using Blish_HUD.Settings;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Threading.Tasks;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

#nullable enable annotations

namespace Orders_From_Above
{

    public class Order
    {
        public string? title { get; set; }
        public string? text { get; set; }
    }

    public class OrderBook
    {
        public string? title { get; set; }
        public string? author { get; set; }
        public string? version { get; set; }

        public List<Order> orders { get; set; }
    }
    [Export(typeof(Blish_HUD.Modules.Module))]
    public class OrdersFromAbove : Blish_HUD.Modules.Module
    {

        private static readonly Logger Logger = Logger.GetLogger<Module>();

        internal static OrdersFromAbove ModuleInstance;
        private CornerIcon _cornerIcon;
        private ContextMenuStrip _cornerMenu;
        private List<OrderBook> _library;

        #region Service Managers
        internal SettingsManager SettingsManager => this.ModuleParameters.SettingsManager;
        internal ContentsManager ContentsManager => this.ModuleParameters.ContentsManager;
        internal DirectoriesManager DirectoriesManager => this.ModuleParameters.DirectoriesManager;
        internal Gw2ApiManager Gw2ApiManager => this.ModuleParameters.Gw2ApiManager;
        #endregion

        [ImportingConstructor]
        public OrdersFromAbove([Import("ModuleParameters")] ModuleParameters moduleParameters) : base(moduleParameters) {
            ModuleInstance = this;
            _library = new List<OrderBook>();
        }

        protected override void DefineSettings(SettingCollection settings)
        {

        }

        protected override void Initialize()
        {
            _cornerIcon = new CornerIcon()
            {
                IconName = "Orders From Above",
                Icon = ContentsManager.GetTexture("icon.png"),
                Priority = 1
            };
            _cornerMenu = new ContextMenuStrip();

            _cornerIcon.Click += delegate
            {
                _cornerMenu.Show(_cornerIcon);
            };
        }
        async Task LoadOrders()
        {
            Logger.Debug("Loading Orders from file.");
            string filepath = DirectoriesManager.GetFullDirectoryPath("orders");
            await LoadOrderBook(Path.Combine(filepath, "orders.yml"));
        }

        void BuildMenu()
        {
            _cornerMenu.ClearChildren();
            Logger.Debug($"Building Orders Menu; found {_library.Count} order(s).");
            foreach (var book in _library)
            {
                var bookItem = _cornerMenu.AddMenuItem(book.title);
                var bookMenu = new ContextMenuStrip();
                foreach (var order in book.orders)
                {
                    var orderItem = bookMenu.AddMenuItem(order.title);
                    orderItem.Click += async delegate { 
                        await ClipboardUtil.WindowsClipboardService.SetTextAsync(order.text); 
                    };

                }
                bookItem.Submenu = bookMenu;
            }

            var reloadMenuItem = _cornerMenu.AddMenuItem("Reload Orders");
            reloadMenuItem.Click += async delegate
            {
                _cornerIcon.LoadingMessage = "Reloading Orders...";
                _library.Clear();
                await LoadOrders();
                BuildMenu();
                _cornerIcon.LoadingMessage = null;
            };

            return;
        }
        protected override async Task LoadAsync()
        {
            await LoadOrders();
            BuildMenu();
        }

        protected override void OnModuleLoaded(EventArgs e)
        {

            // Base handler must be called
            base.OnModuleLoaded(e);
        }

        protected override void Update(GameTime gameTime)
        {

        }

        /// <inheritdoc />
        protected override void Unload()
        {
            // Unload here

            // All static members must be manually unset
        }

        public async Task LoadOrderBook(string OrdersPath)
        {
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();
            try
            {
                using var ordersReader = File.OpenText(OrdersPath);
                var text = await ordersReader.ReadToEndAsync();
                var book = deserializer.Deserialize<OrderBook>(text);
                
                _library.Add(book);
            }
            catch (Exception e)
            {
                Logger.Warn(e, $"Failed to load orders from file at {OrdersPath}. {e}");
            }
        }

    }

}
