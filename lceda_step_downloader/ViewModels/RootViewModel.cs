using Stylet;
using lceda_step_downloader.Models.Root;
using lceda_step_downloader.Models.Component;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Controls;
using System.Diagnostics;
using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using HelixToolkit.Wpf;
using System.Windows.Media.Media3D;
using System.Windows.Data;
using System.Globalization;
using System.IO;
using System.Windows.Threading;
using System.Windows;
using System.Text;
using System.IO.Compression;
using System.Net;
using System.Linq;
using HandyControl.Controls;

namespace lceda_step_downloader.ViewModels
{
    public class RootViewModel : PropertyChangedBase
    {
        private string _title = "立创EDA 3D模型下载器";
        public string Title
        {
            get { return _title; }
            set { SetAndNotify(ref _title, value); }
        }
        private static readonly HttpClient client = new(new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.GZip
        });

        private bool _downloadallowed;
        public bool DownloadAllowed
        {
            get { return _downloadallowed; }
            set { SetAndNotify(ref _downloadallowed, value); }
        }

        private ResultItem _selecteditem;
        public ResultItem Selecteditem
        {
            get { return _selecteditem; }
            set { SetAndNotify(ref _selecteditem, value); }
        }

        private Model3DGroup _myModelGroup;
        public Model3DGroup MyModelGroup
        {
            get { return _myModelGroup; }
            set
            {
                SetAndNotify(ref _myModelGroup, value);

            }
        }

        private string _imageSource;

        public string ImageSource
        {
            get => _imageSource;
            set => SetAndNotify(ref _imageSource, value);
        }

        private Root searchresult;
        public Root SearchResult
        {
            get { return searchresult; }
            set { SetAndNotify(ref searchresult, value); }
        }

        private Component _selectedcomponent;
        public Component SelectedComponent
        {
            get { return _selectedcomponent; }
            set { SetAndNotify(ref _selectedcomponent, value); }
        }
        private ObservableCollection<SearchSite> _searchsites;

        public ObservableCollection<SearchSite> SearchSites
        {
            get { return _searchsites; }
            set { _searchsites = value; }
        }
        private SearchSite _ssite;

        public SearchSite SSite
        {
            get { return _ssite; }
            set
            {
                _ssite = value;
                Debug.WriteLine(value.Site);
            }
        }

        private string _schematicSvgPath;
        public string SchematicSvgPath
        {
            get => _schematicSvgPath;
            set
            {
                SetAndNotify(ref _schematicSvgPath, value);
                SchematicSvgUri = value != null ? new Uri("file:///" + value.Replace("\\", "/")) : null;
            }
        }

        private Uri _schematicSvgUri;
        public Uri SchematicSvgUri
        {
            get => _schematicSvgUri;
            set => SetAndNotify(ref _schematicSvgUri, value);
        }

        private string _footprintSvgPath;
        public string FootprintSvgPath
        {
            get => _footprintSvgPath;
            set
            {
                SetAndNotify(ref _footprintSvgPath, value);
                FootprintSvgUri = value != null ? new Uri("file:///" + value.Replace("\\", "/")) : null;
            }
        }

        private Uri _footprintSvgUri;
        public Uri FootprintSvgUri
        {
            get => _footprintSvgUri;
            set => SetAndNotify(ref _footprintSvgUri, value);
        }

        private Uri _schematicEmbedUri;
        public Uri SchematicEmbedUri
        {
            get => _schematicEmbedUri;
            set => SetAndNotify(ref _schematicEmbedUri, value);
        }

        private Uri _footprintEmbedUri;
        public Uri FootprintEmbedUri
        {
            get => _footprintEmbedUri;
            set => SetAndNotify(ref _footprintEmbedUri, value);
        }

        private bool _hasSchematic;
        public bool HasSchematic
        {
            get => _hasSchematic;
            set => SetAndNotify(ref _hasSchematic, value);
        }

        private bool _hasFootprint;
        public bool HasFootprint
        {
            get => _hasFootprint;
            set => SetAndNotify(ref _hasFootprint, value);
        }

        private bool _hasDatasheet;
        public bool HasDatasheet
        {
            get => _hasDatasheet;
            set => SetAndNotify(ref _hasDatasheet, value);
        }

        private bool _has3DModel;
        public bool Has3DModel
        {
            get => _has3DModel;
            set => SetAndNotify(ref _has3DModel, value);
        }

        private string _datasheetUrl;
        public string DatasheetUrl
        {
            get => _datasheetUrl;
            set => SetAndNotify(ref _datasheetUrl, value);
        }

        public RootViewModel()
        {
            Selecteditem = null;
            SearchSites = new ObservableCollection<SearchSite>()
            {
              //new SearchSite(){Site="LCEDA", Value = 0},
              new SearchSite(){Site="LCSC", Value = 1},
            };
            SSite = SearchSites[0];

            //创建模型存储目录
            if (!Directory.Exists(@".\temp"))
            {
                Directory.CreateDirectory(@".\temp");
            }
            if (!Directory.Exists(@".\step"))
            {
                Directory.CreateDirectory(@".\step");
            }
            if (!Directory.Exists(@".\symbols"))
            {
                Directory.CreateDirectory(@".\symbols");
            }
            if (!Directory.Exists(@".\footprints"))
            {
                Directory.CreateDirectory(@".\footprints");
            }
            if (!Directory.Exists(@".\datasheets"))
            {
                Directory.CreateDirectory(@".\datasheets");
            }
        }

        public void DoSearch(string argument)
        {
            Debug.WriteLine(String.Format("搜索关键字: {0}", argument));
            Task task = new(() => SearchTask(argument));
            task.Start();
        }

        public async void SearchTask(string argument)
        {
            if (SSite == null)
            {
                return;
            }
            if (SSite.Site == "LCSC")
            {
                var streamTask = client.GetStreamAsync("https://pro.lceda.cn/api/szlcsc/eda/product/list?wd=" + argument.ToString());
                Debug.WriteLine(streamTask.ToString());
                SearchResult = await JsonSerializer.DeserializeAsync<Root>(await streamTask);
                Debug.WriteLine(SearchResult.result.Count);

                // 获取价格信息
                await FetchPricesAsync();
            }

        }

        private async Task FetchPricesAsync()
        {
            if (SearchResult?.result == null) return;

            foreach (var item in SearchResult.result)
            {
                try
                {
                    var payload = new
                    {
                        keyword = item.product_code,
                        currentPage = 1,
                        pageSize = 10
                    };
                    var content = new StringContent(
                        JsonSerializer.Serialize(payload),
                        Encoding.UTF8,
                        "application/json");
                    var response = await client.PostAsync(
                        "https://jlcpcb.com/api/overseas-pcb-order/v1/shoppingCart/smtGood/selectSmtComponentList",
                        content);
                    if (response.IsSuccessStatusCode)
                    {
                        var jsonContent = await response.Content.ReadAsStringAsync();
                        var result = JsonSerializer.Deserialize<JsonElement>(jsonContent);
                        if (result.GetProperty("code").GetInt32() == 200)
                        {
                            var list = result.GetProperty("data").GetProperty("componentPageInfo").GetProperty("list");
                            if (list.GetArrayLength() > 0)
                            {
                                var prices = list[0].GetProperty("componentPrices");
                                if (prices.GetArrayLength() > 0)
                                {
                                    var priceBuilder = new StringBuilder();
                                    priceBuilder.AppendLine("价格梯度：");
                                    foreach (var price in prices.EnumerateArray())
                                    {
                                        var start = price.GetProperty("startNumber").GetInt32();
                                        var end = price.GetProperty("endNumber").GetInt32();
                                        var unitPrice = price.GetProperty("productPrice").GetDouble();
                                        var endStr = end == -1 ? "更多" : end.ToString();
                                        priceBuilder.AppendLine($"{start}-{endStr}: ¥{unitPrice:F4}");
                                    }
                                    item.PriceInfo = priceBuilder.ToString().TrimEnd();
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"获取价格失败 {item.product_code}: {ex.Message}");
                }
            }
        }

        public void OnResultSelection()
        {
            if (Selecteditem == null)
            {
                return;
            }
            DownloadAllowed = true;

            //部分器件没有图片, 替换成商城LOGO
            if (Selecteditem.images.Count == 0)
            {
                ImageSource = "https:" + Selecteditem.creator.avatar;
            }
            else
            {
                // 尝试获取大图，将 middle 替换为 large
                var imageUrl = Selecteditem.images[0];
                if (imageUrl.Contains("/middle/"))
                {
                    var largeUrl = imageUrl.Replace("/middle/", "/large/");
                    // 验证大图是否存在
                    try
                    {
                        var headResponse = client.SendAsync(new HttpRequestMessage(HttpMethod.Head, largeUrl)).Result;
                        if (headResponse.IsSuccessStatusCode)
                        {
                            imageUrl = largeUrl;
                        }
                    }
                    catch { }
                }
                ImageSource = imageUrl;
            }

            //原理图可用性检测
            HasSchematic = Selecteditem.symbol != null && !string.IsNullOrEmpty(Selecteditem.symbol.uuid);
            SchematicSvgPath = null;

            //封装可用性检测
            HasFootprint = Selecteditem.footprint != null && !string.IsNullOrEmpty(Selecteditem.footprint.uuid);
            FootprintSvgPath = null;

            //规格书可用性检测
            HasDatasheet = !string.IsNullOrEmpty(Selecteditem.attributes?.Datasheet);
            DatasheetUrl = Selecteditem.attributes?.Datasheet;

            //3D模型可用性检测
            Has3DModel = !string.IsNullOrEmpty(Selecteditem.attributes?._3D_Model);

            //加载原理图和封装预览
            LoadSchematicPreview();
            LoadFootprintPreview();

            //自动加载3D模型
            if (Has3DModel)
            {
                DownloadObj();
            }
        }

        public void DownloadObj()
        {
            if (Selecteditem == null)
            {
                return;
            }

            Debug.WriteLine("准备下载obj:编号{0},标题{1}", SearchResult.result.IndexOf(Selecteditem), Selecteditem.display_title);
            Debug.WriteLine(Selecteditem.attributes._3D_Model);

            if (File.Exists(@".\temp\" + Selecteditem.title.ToString().Replace("/", "") + @".obj"))
            {
                Debug.WriteLine("存在缓存");
                Application.Current.Dispatcher.Invoke(() =>
                {
                    ObjReader CurrentHelixObjReader = new();
                    MyModelGroup = CurrentHelixObjReader.Read(@".\temp\" + Selecteditem.title.ToString().Replace("/", "") + @".obj");

                });
                return;
            }
            Task.Run(() => DownloadObjAsync());
        }

        public void DownloadStep()
        {
            //https://pro.lceda.cn/api/components/9059586b8e0c4e2ba21b2ac2c1eb066b?uuid=9059586b8e0c4e2ba21b2ac2c1eb066b&path=0819f05c4eef4c71ace90d822a990e87
            //https://pro.lceda.cn/api/components/105b388c0c03439aa7dbf35dd2b762a6?uuid=105b388c0c03439aa7dbf35dd2b762a6
            //{"success":true,"code":0,"result":{"uuid":"105b388c0c03439aa7dbf35dd2b762a6","modifier":{"uuid":"0819f05c4eef4c71ace90d822a990e87","username":"LCSC","nickname":"LCSC","avatar":"\/\/image.lceda.cn\/avatars\/2018\/6\/kFlrasi7W06gTdBLAqW3fkrqbDhbowynuSzkjqso.png"},"creator":{"uuid":"0819f05c4eef4c71ace90d822a990e87","username":"LCSC","nickname":"LCSC","avatar":"\/\/image.lceda.cn\/avatars\/2018\/6\/kFlrasi7W06gTdBLAqW3fkrqbDhbowynuSzkjqso.png"},"owner":{"uuid":"0819f05c4eef4c71ace90d822a990e87","username":"LCSC","nickname":"LCSC","avatar":"\/\/image.lceda.cn\/avatars\/2018\/6\/kFlrasi7W06gTdBLAqW3fkrqbDhbowynuSzkjqso.png"},"description":"","docType":16,"dataStr":"{\"model\":\"6d30b5a04660477fbdff168686b01590\",\"type\":\"wrl\",\"src\":\"qfn-56_l7.0-w7.0-p0.40-tl-ep4.0\",\"unit\":\"mm\"}","tags":{"parent_tag":[],"child_tag":[]},"public":true,"source":"","version":1653017104,"type":3,"title":"qfn-56_l7.0-w7.0-p0.40-tl-ep4.0","createTime":1653017104,"updateTime":1658962217,"created_at":"2022-05-20 11:25:04","display_title":"QFN-56_L7.0-W7.0-P0.40-TL-EP4.0","updated_at":"2022-07-28 06:55:05","ticket":1,"std_uuid":"ce2b808f96c74d7981784d534cecd1c0","3d_model_uuid":"6d30b5a04660477fbdff168686b01590","has_device":false,"path":"0819f05c4eef4c71ace90d822a990e87"}}
            //https://modules.lceda.cn/qAxj6KHrDKw4blvCG8QJPs7Y/6d30b5a04660477fbdff168686b01590
            if (Selecteditem == null)
            {
                return;
            }

            Debug.WriteLine("准备下载step:编号{0},标题{1}", SearchResult.result.IndexOf(Selecteditem), Selecteditem.display_title);
            Debug.WriteLine(Selecteditem.attributes._3D_Model_Transform);

            //器件名称
            //if (File.Exists(@".\step\" + Selecteditem.title.ToString().Replace("/", "") + @".step"))
            //封装名称
            if (File.Exists(@".\step\" + Selecteditem.footprint.display_title.ToString().Replace("/", "") + @".step"))
            {
                Debug.WriteLine("存在step缓存");
                ShowFileExistsNotification(Path.Combine(AppContext.BaseDirectory, "step"), "STEP文件已存在");
                return;
            }
            DownloadAllowed = false;
            Task.Run(() => DownloadStepAsync());
        }

        public void OpenStepFolder()
        {
            OpenDirectory(Path.Combine(AppContext.BaseDirectory, "step"));
        }

        public void OpenSymbolsFolder()
        {
            OpenDirectory(AppContext.BaseDirectory);
        }

        //构造PCB数据, 以利用lceda专业版的PCB导出STEP接口
        public async void DownloadStepAsync()
        {
            var streamTask = client.GetStreamAsync("https://pro.lceda.cn/api/components/" + Selecteditem.attributes._3D_Model + "?uuid=" + Selecteditem.attributes._3D_Model);

            SelectedComponent = await JsonSerializer.DeserializeAsync<Component>(await streamTask);
            if (SelectedComponent.code != 0)
            {
                SelectedComponent = new Component
                {
                    result = new Result()
                };
                SelectedComponent.result._3d_model_uuid = Selecteditem.attributes._3D_Model;
            }
            Debug.WriteLine(SelectedComponent.result._3d_model_uuid);

            Stream streamStep = await client.GetStreamAsync("https://modules.lceda.cn/qAxj6KHrDKw4blvCG8QJPs7Y/" + SelectedComponent.result._3d_model_uuid);
            //器件名称
            //var tempTitle = string.Join("_", Selecteditem.title.ToString().Split(Path.GetInvalidFileNameChars()));
            //封装名称
            var tempTitle = string.Join("_", Selecteditem.footprint.display_title.ToString().ToString().Split(Path.GetInvalidFileNameChars()));
            string fileToWriteTo = Path.Combine(AppContext.BaseDirectory, "step", tempTitle + ".step");
            using Stream streamToWriteTo = File.Open(fileToWriteTo, FileMode.Create);
            await streamStep.CopyToAsync(streamToWriteTo);
            //MediaEle sr = new(await streamStep);
            //using Stream streamToWriteTo = File.Open(@".\step\" + Selecteditem.title.ToString().Replace("/", "") + @".step", FileMode.Create);
            //await sr.CopyToAsync(streamToWriteTo);

            //StreamWriter stepWriter = new(@".\step\" + Selecteditem.title.ToString().Replace("/", "") + @".step");

            //器件模型的变换数据, 以适应lceda的坐标系以及比例, 参数由lc后台维护, 可见lceda的模型也不全是自己画的
            var model_dx = Convert.ToDouble(Selecteditem.attributes._3D_Model_Transform.Split(',')[0]) / 10.0;
            var model_dy = Convert.ToDouble(Selecteditem.attributes._3D_Model_Transform.Split(',')[1]) / 10.0;
            var model_dz = Convert.ToDouble(Selecteditem.attributes._3D_Model_Transform.Split(',')[2]) / 10.0;
            var model_rz = Convert.ToInt32(Selecteditem.attributes._3D_Model_Transform.Split(',')[3]);
            var model_rx = Convert.ToInt32(Selecteditem.attributes._3D_Model_Transform.Split(',')[4]);
            var model_ry = Convert.ToInt32(Selecteditem.attributes._3D_Model_Transform.Split(',')[5]);
            var model_x = Convert.ToDouble(Selecteditem.attributes._3D_Model_Transform.Split(',')[6]) / 10.0;
            var model_y = Convert.ToDouble(Selecteditem.attributes._3D_Model_Transform.Split(',')[7]) / 10.0;
            var model_z = Convert.ToDouble(Selecteditem.attributes._3D_Model_Transform.Split(',')[8]) / 10.0 - 49;

            //构造的PCB数据
            //var stringPayload =
            //    "[\"DOCTYPE\",\"PREVIEW\",\"1.0\"]\r\n" +
            //    "[\"HEAD\",{\"scale\":0.0254}]\r\n" +
            //    "[\"HEAD\",{\"scale\":10}]\r\n" +
            //    "[\"LAYER\",11,\"OUTLINE\",\"Board Outline Layer\",3,\"#c2c200\",1,\"#c2c200\",1]\r\n" +
            //    "[\"POLY\",\"e60\",0,\"\",11,1,[40,-40,\"L\",42,-40,42,-38,40,-38,40,-40],0]\r\n" +
            //    "[\"COMPONENT\",\"e0\",0,9,0,0,0,{\"uuid\":\"" + SelectedComponent.result._3d_model_uuid +
            //    "\",\"dx\":" + model_dx.ToString() +
            //    ",\"dy\":" + model_dy.ToString() +
            //    ",\"dz\":" + model_dz.ToString() +
            //    ",\"rz\":" + model_rz.ToString() +
            //    ",\"rx\":" + model_rx.ToString() +
            //    ",\"ry\":" + model_ry.ToString() +
            //    ",\"x\":" + model_x.ToString() +
            //    ",\"y\":" + model_y.ToString() +
            //    ",\"z\":" + model_z.ToString() +
            //    ",\"Footprint\":\"USB-C-SMD_TYPEC-303-ACP16\",\"Designator\":\"USB1\",\"Device\":\"TYPEC-303-ACP16\"},0]";

            //var compressedContent = CompressRequestContent(stringPayload);
            //compressedContent.Headers.Add("Content-Encoding", "gzip");
            //compressedContent.Headers.Add("Content-Type", "x-application/x-gzip");

            //var resp = await client.PostAsync("https://pro.lceda.cn/occapi/api/convert/pcb2step", compressedContent);

            //var responseStream = await resp.Content.ReadAsStreamAsync();

            //var streamReader = new StreamReader(responseStream);

            //StreamWriter stepWriter = new(@".\step\" + Selecteditem.title.ToString().Replace("/", "") + @".step");

            //如果你上传的PCB数据里只有器件没有PCB, 或者PCB面积过小(<0.5*0.5mm), lc后台都会给你加上PCB
            //所以这里用了个凑活能用的方法:在程序里自动删掉PCB对应实体节点, 可能在某些软件里仍然会显示一个小小的PCB
            //实测AD Fusion 360 SW显示正常, 有更好的方法欢迎PR
            //string readline;
            //while ((readline = streamReader.ReadLine()) != null)
            //{
            //    if(readline.Contains("#29 = ADVANCED_BREP_SHAPE_REPRESENTATION('',(#11,#30)"))
            //    {
            //        stepWriter.WriteLine(readline.Replace("#30", "'NONE'"));
            //    }
            //    else
            //    {
            //        stepWriter.WriteLine(readline);
            //    }
            //}

            //stepWriter.Flush();
            //stepWriter.Close();
            //stepWriter.Dispose();
            DownloadAllowed = true;

            Application.Current.Dispatcher.Invoke(() =>
                ShowDownloadSuccessNotification(Path.Combine(AppContext.BaseDirectory, "step")));
        }

        public static HttpContent CompressRequestContent(string content)
        {
            var compressedStream = new MemoryStream();
            using (var contentStream = new System.IO.MemoryStream(Encoding.UTF8.GetBytes(content)))
            {
                using var gzipStream = new GZipStream(compressedStream, CompressionMode.Compress);
                contentStream.CopyTo(gzipStream);
            }

            var httpContent = new ByteArrayContent(compressedStream.ToArray());
            return httpContent;
        }

        public async void DownloadObjAsync()
        {
            var streamTask = client.GetStreamAsync("https://pro.lceda.cn/api/components/" + Selecteditem.attributes._3D_Model + "?uuid=" + Selecteditem.attributes._3D_Model);

            SelectedComponent = await JsonSerializer.DeserializeAsync<Component>(await streamTask);
            if (SelectedComponent.code != 0)
            {
                SelectedComponent = new Component
                {
                    result = new Result
                    {
                        _3d_model_uuid = Selecteditem.attributes._3D_Model
                    }
                };
            }
            Debug.WriteLine(SelectedComponent.result._3d_model_uuid);

            var streamObj = client.GetStreamAsync("https://modules.lceda.cn/3dmodel/" + SelectedComponent.result._3d_model_uuid);
            ObjMtlSplit(streamObj);
        }

        //lc前端从服务端获取的OBJ模型数据实际上是把mtl和obj写在了一个文件里面, 然后前端再做处理展示, 这里同样需要做分离
        public async void ObjMtlSplit(Task<Stream> objstream)
        {
            var tempTitle = string.Join("_", Selecteditem.title.ToString().Split(Path.GetInvalidFileNameChars()));
            StreamWriter objWriter = new(Path.Combine(AppContext.BaseDirectory, "temp", tempTitle + ".obj"));
            StreamWriter mtlWriter = new(Path.Combine(AppContext.BaseDirectory, "temp", tempTitle + ".mtl"));
            //StreamWriter objWriter = new(@".\temp\" + Selecteditem.title.ToString().Replace("/", "") + @".obj");
            //StreamWriter mtlWriter = new(@".\temp\" + Selecteditem.title.ToString().Replace("/", "") + @".mtl");

            objWriter.WriteLine("mtllib " + tempTitle + ".mtl");
            StreamReader sr = new(await objstream);
            String readline = string.Empty;
            while ((readline = sr.ReadLine()) != null)
            {
                objWriter.WriteLine(readline);
                if (readline.Contains("newmtl"))
                {
                    mtlWriter.WriteLine(readline);
                    for (var i = 0; i < 3; i++)
                    {
                        readline = sr.ReadLine();
                        mtlWriter.WriteLine(readline);
                    }
                    readline = sr.ReadLine();
                    readline = sr.ReadLine();
                    mtlWriter.WriteLine(readline);
                }
            }
            mtlWriter.Flush();
            mtlWriter.Close();
            objWriter.Flush();
            objWriter.Close();
            Application.Current.Dispatcher.Invoke(() =>
            {
                ObjReader CurrentHelixObjReader = new();
                MyModelGroup = CurrentHelixObjReader.Read(Path.Combine(AppContext.BaseDirectory, "temp", tempTitle + ".obj"));
            });
        }

        public void LoadSchematicPreview()
        {
            if (!HasSchematic) return;
            Task.Run(() => FetchSchematicAsync());
        }

        private async Task FetchSchematicAsync()
        {
            try
            {
                var productCode = Selecteditem.product_code;
                Debug.WriteLine($"获取原理图 SVG: {productCode}");

                // 设置 EasyEDA 嵌入式查看器 URI
                var symbolUuid = Selecteditem.symbol?.uuid;
                if (!string.IsNullOrEmpty(symbolUuid))
                {
                    Application.Current.Dispatcher.Invoke(() =>
                        SchematicEmbedUri = new Uri($"https://easyeda.com/embed/{symbolUuid}"));
                }

                // 使用 EasyEDA 的 SVG API
                var svgUrl = $"https://easyeda.com/api/products/{productCode}/svgs";
                Debug.WriteLine($"尝试 URL: {svgUrl}");
                var response = await client.GetAsync(svgUrl);
                if (response.IsSuccessStatusCode)
                {
                    var jsonContent = await response.Content.ReadAsStringAsync();
                    Debug.WriteLine($"API 返回长度: {jsonContent.Length}");

                    var svgResponse = JsonSerializer.Deserialize<JsonElement>(jsonContent);
                    if (svgResponse.GetProperty("success").GetBoolean())
                    {
                        var results = svgResponse.GetProperty("result");
                        if (results.GetArrayLength() > 0)
                        {
                            var svgContent = results[0].GetProperty("svg").GetString();
                            if (!string.IsNullOrEmpty(svgContent))
                            {
                                var tempPath = Path.Combine(AppContext.BaseDirectory, "temp", $"schematic_{productCode}.svg");
                                await File.WriteAllTextAsync(tempPath, svgContent, Encoding.UTF8);
                                Application.Current.Dispatcher.Invoke(() => SchematicSvgPath = tempPath);
                                Debug.WriteLine("原理图 SVG 保存成功");
                                return;
                            }
                        }
                    }
                }

                Debug.WriteLine("无法获取原理图数据");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"获取原理图失败: {ex.Message}");
                Debug.WriteLine($"堆栈跟踪: {ex.StackTrace}");
            }
        }

        public void LoadFootprintPreview()
        {
            if (!HasFootprint) return;
            Task.Run(() => FetchFootprintAsync());
        }

        private async Task FetchFootprintAsync()
        {
            try
            {
                var productCode = Selecteditem.product_code;
                Debug.WriteLine($"获取封装 SVG: {productCode}");

                // 设置 EasyEDA 嵌入式查看器 URI
                var footprintUuid = Selecteditem.footprint?.uuid;
                if (!string.IsNullOrEmpty(footprintUuid))
                {
                    Application.Current.Dispatcher.Invoke(() =>
                        FootprintEmbedUri = new Uri($"https://easyeda.com/embed/{footprintUuid}"));
                }

                // 使用 EasyEDA 的 SVG API
                var svgUrl = $"https://easyeda.com/api/products/{productCode}/svgs";
                Debug.WriteLine($"尝试 URL: {svgUrl}");
                var response = await client.GetAsync(svgUrl);
                if (response.IsSuccessStatusCode)
                {
                    var jsonContent = await response.Content.ReadAsStringAsync();
                    Debug.WriteLine($"API 返回长度: {jsonContent.Length}");

                    var svgResponse = JsonSerializer.Deserialize<JsonElement>(jsonContent);
                    if (svgResponse.GetProperty("success").GetBoolean())
                    {
                        var results = svgResponse.GetProperty("result");
                        if (results.GetArrayLength() > 1)
                        {
                            // 最后一个元素是封装
                            var svgContent = results[results.GetArrayLength() - 1].GetProperty("svg").GetString();
                            if (!string.IsNullOrEmpty(svgContent))
                            {
                                var tempPath = Path.Combine(AppContext.BaseDirectory, "temp", $"footprint_{productCode}.svg");
                                await File.WriteAllTextAsync(tempPath, svgContent, Encoding.UTF8);
                                Application.Current.Dispatcher.Invoke(() => FootprintSvgPath = tempPath);
                                Debug.WriteLine("封装 SVG 保存成功");
                                return;
                            }
                        }
                    }
                }

                Debug.WriteLine("无法获取封装数据");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"获取封装失败: {ex.Message}");
                Debug.WriteLine($"堆栈跟踪: {ex.StackTrace}");
            }
        }

        public void DownloadSchematic()
        {
            if (Selecteditem == null || !HasSchematic) return;

            var fileName = string.Join("_",
                Selecteditem.symbol.display_title.ToString().Split(Path.GetInvalidFileNameChars()));
            var filePath = Path.Combine(AppContext.BaseDirectory, "symbols", fileName + ".svg");

            if (File.Exists(filePath))
            {
                ShowFileExistsNotification(Path.GetDirectoryName(filePath), "原理图文件已存在");
                return;
            }

            Task.Run(() => DownloadSchematicAsync(filePath));
        }

        private async Task DownloadSchematicAsync(string filePath)
        {
            try
            {
                var productCode = Selecteditem.product_code;
                string svgContent = null;

                // 使用 EasyEDA 的 SVG API
                var svgUrl = $"https://easyeda.com/api/products/{productCode}/svgs";
                var response = await client.GetAsync(svgUrl);
                if (response.IsSuccessStatusCode)
                {
                    var jsonContent = await response.Content.ReadAsStringAsync();
                    var svgResponse = JsonSerializer.Deserialize<JsonElement>(jsonContent);
                    if (svgResponse.GetProperty("success").GetBoolean())
                    {
                        var results = svgResponse.GetProperty("result");
                        if (results.GetArrayLength() > 0)
                        {
                            svgContent = results[0].GetProperty("svg").GetString();
                        }
                    }
                }

                if (string.IsNullOrEmpty(svgContent))
                {
                    Application.Current.Dispatcher.Invoke(() =>
                        Growl.Error("无法获取原理图数据"));
                    return;
                }

                await File.WriteAllTextAsync(filePath, svgContent, Encoding.UTF8);

                Application.Current.Dispatcher.Invoke(() =>
                    ShowDownloadSuccessNotification(Path.GetDirectoryName(filePath)));
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                    Growl.Error($"原理图下载失败: {ex.Message}"));
            }
        }

        public void DownloadFootprint()
        {
            if (Selecteditem == null || !HasFootprint) return;

            var fileName = string.Join("_",
                Selecteditem.footprint.display_title.ToString().Split(Path.GetInvalidFileNameChars()));
            var filePath = Path.Combine(AppContext.BaseDirectory, "footprints", fileName + ".svg");

            if (File.Exists(filePath))
            {
                ShowFileExistsNotification(Path.GetDirectoryName(filePath), "封装文件已存在");
                return;
            }

            Task.Run(() => DownloadFootprintAsync(filePath));
        }

        private async Task DownloadFootprintAsync(string filePath)
        {
            try
            {
                var productCode = Selecteditem.product_code;
                string svgContent = null;

                // 使用 EasyEDA 的 SVG API
                var svgUrl = $"https://easyeda.com/api/products/{productCode}/svgs";
                var response = await client.GetAsync(svgUrl);
                if (response.IsSuccessStatusCode)
                {
                    var jsonContent = await response.Content.ReadAsStringAsync();
                    var svgResponse = JsonSerializer.Deserialize<JsonElement>(jsonContent);
                    if (svgResponse.GetProperty("success").GetBoolean())
                    {
                        var results = svgResponse.GetProperty("result");
                        if (results.GetArrayLength() > 1)
                        {
                            // 最后一个元素是封装
                            svgContent = results[results.GetArrayLength() - 1].GetProperty("svg").GetString();
                        }
                    }
                }

                if (string.IsNullOrEmpty(svgContent))
                {
                    Application.Current.Dispatcher.Invoke(() =>
                        Growl.Error("无法获取封装数据"));
                    return;
                }

                await File.WriteAllTextAsync(filePath, svgContent, Encoding.UTF8);

                Application.Current.Dispatcher.Invoke(() =>
                    ShowDownloadSuccessNotification(Path.GetDirectoryName(filePath)));
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                    Growl.Error($"封装下载失败: {ex.Message}"));
            }
        }

        public void OpenDatasheet()
        {
            if (!HasDatasheet || string.IsNullOrEmpty(DatasheetUrl)) return;

            var url = DatasheetUrl;
            // 确保 URL 以 http 开头
            if (!url.StartsWith("http://") && !url.StartsWith("https://"))
            {
                url = "https:" + url;
            }

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                Growl.Error($"无法打开规格书: {ex.Message}");
            }
        }

        public void OpenDatasheetFromList(ResultItem item)
        {
            if (item == null) return;

            var datasheetUrl = item.attributes?.Datasheet;
            if (string.IsNullOrEmpty(datasheetUrl))
            {
                Growl.Warning("该器件暂无数据手册");
                return;
            }

            // 确保 URL 以 http 开头
            if (!datasheetUrl.StartsWith("http://") && !datasheetUrl.StartsWith("https://"))
            {
                datasheetUrl = "https:" + datasheetUrl;
            }

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = datasheetUrl,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                Growl.Error($"无法打开数据手册: {ex.Message}");
            }
        }

        public void DownloadDatasheet()
        {
            if (!HasDatasheet || string.IsNullOrEmpty(DatasheetUrl)) return;

            var url = DatasheetUrl;
            // 确保 URL 以 http 开头
            if (!url.StartsWith("http://") && !url.StartsWith("https://"))
            {
                url = "https:" + url;
            }

            var fileName = string.Join("_",
                Selecteditem.display_title.ToString().Split(Path.GetInvalidFileNameChars())) + ".pdf";
            var filePath = Path.Combine(AppContext.BaseDirectory, "datasheets", fileName);

            if (File.Exists(filePath))
            {
                ShowFileExistsNotification(Path.GetDirectoryName(filePath), "规格书文件已存在");
                return;
            }

            Task.Run(() => DownloadDatasheetAsync(url, filePath));
        }

        private async Task DownloadDatasheetAsync(string url, string filePath)
        {
            try
            {
                var response = await client.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                        Growl.Error("无法下载规格书"));
                    return;
                }

                await using var stream = await response.Content.ReadAsStreamAsync();
                await using var fileStream = File.Open(filePath, FileMode.Create);
                await stream.CopyToAsync(fileStream);

                Application.Current.Dispatcher.Invoke(() =>
                    ShowDownloadSuccessNotification(Path.GetDirectoryName(filePath)));
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                    Growl.Error($"规格书下载失败: {ex.Message}"));
            }
        }

        private void ShowFileExistsNotification(string directoryPath, string message)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                Growl.Ask(new HandyControl.Data.GrowlInfo
                {
                    Message = message,
                    ConfirmStr = "打开文件夹",
                    CancelStr = "关闭",
                    IsCustom = true,
                    IconKey = "SuccessGeometry",
                    IconBrushKey = "SuccessBrush",
                    ActionBeforeClose = isConfirmed =>
                    {
                        if (isConfirmed)
                            OpenDirectory(directoryPath);
                        return true;
                    }
                });
            });
        }

        private void ShowDownloadSuccessNotification(string directoryPath)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                Growl.Ask(new HandyControl.Data.GrowlInfo
                {
                    Message = "下载成功！是否打开文件夹？",
                    ConfirmStr = "打开文件夹",
                    CancelStr = "关闭",
                    IsCustom = true,
                    IconKey = "SuccessGeometry",
                    IconBrushKey = "SuccessBrush",
                    ActionBeforeClose = isConfirmed =>
                    {
                        if (isConfirmed)
                            OpenDirectory(directoryPath);
                        return true;
                    }
                });
            });
        }

        private void OpenDirectory(string path)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = path,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                Growl.Error("无法打开文件夹: " + ex.Message);
            }
        }
    }

    [ValueConversion(typeof(Int32), typeof(ListViewItem))]
    public class IndexConverter : IValueConverter
    {
        public object Convert(object value, Type TargetType, object parameter, CultureInfo culture)
        {
            ListViewItem item = (ListViewItem)value;
            ListView listView = ItemsControl.ItemsControlFromItemContainer(item) as ListView;
            return listView.ItemContainerGenerator.IndexFromContainer(item) + 1;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BooleanOrConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            return !values.OfType<bool>().Any((b => b == false));

        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
