using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace ElectronicsShop.Controllers
{
    public class ChatController : Controller
    {
        private static readonly HttpClient _http = new HttpClient();

        [HttpPost]
        public async Task<JsonResult> Send(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return Json(new { ok = false, error = "Empty" });

            var msg = message.ToLower();
            var conn = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
            TrackBehavior(msg);
            Session["last_msg"] = msg;

            try
            {
                // ===== SMART INTENT =====

                if (msg.Contains("bán chạy nhất") || msg.Contains("hot"))
                    return Json(new { ok = true, reply = GetBestSellerSafe(conn) });

                if (msg.Contains("mới"))
                    return Json(new { ok = true, reply = GetNewestSafe(conn) });

                if (msg.Contains("không bán") || msg.Contains("chưa bán"))
                    return Json(new { ok = true, reply = GetUnsoldSafe(conn) });

                if (msg.Contains("danh mục"))
                    return Json(new { ok = true, reply = GetCategoriesSafe(conn) });

                if (msg.Contains("đơn hàng"))
                    return Json(new { ok = true, reply = GetOrdersSafe(conn) });

                if (msg.Contains("khách"))
                    return Json(new { ok = true, reply = GetCustomersSafe(conn) });

                if (msg.Contains("cấu hình") || msg.Contains("thông số"))
                    return Json(new { ok = true, reply = GetSpecsSafe(conn) });

                if (msg.Contains("rẻ"))
                    return Json(new { ok = true, reply = GetCheapSafe(conn) });

                if (msg.Contains("đắt"))
                    return Json(new { ok = true, reply = GetExpensiveSafe(conn) });

                if (msg.Contains("thống kê"))
                {
                    var orders = GetOrdersSafe(conn);
                    var best = GetBestSeller(conn); // ❗ gọi trực tiếp, không dùng Safe

                    return Json(new
                    {
                        ok = true,
                        reply = $"📊 Thống kê hệ thống:\n{orders}\n\n{best}"
                    });
                }

                if (msg.Contains("so sánh hãng"))
                {
                    var compareBrand = CompareBrandsByCategory(conn, msg);
                    if (compareBrand != null)
                        return Json(new { ok = true, reply = compareBrand });
                }

                if (msg.Contains("so sánh"))
                {
                    var compare = CompareProducts(conn, msg);
                    if (compare != null)
                        return Json(new { ok = true, reply = compare });
                }

                if (msg.Contains("mua") || msg.Contains("thêm"))
                {
                    return Json(new { ok = true, reply = AddToCart(conn, msg) });
                }

                if (msg.Contains("giỏ"))
                    return Json(new { ok = true, reply = ViewCart() });

                // 👉 GỢI Ý THEO HÀNH VI
                var behavior = RecommendByBehavior(conn, msg);
                if (!string.IsNullOrEmpty(behavior))
                    return Json(new { ok = true, reply = behavior });

                var filter = GetByCategoryOrBrand(conn, msg);
                if (!string.IsNullOrEmpty(filter))
                    return Json(new { ok = true, reply = filter });

                var smartIntent = DetectIntentSmart(msg);
                if (smartIntent == "samsung")
                    return Json(new { ok = true, reply = GetProductSmart(conn, "samsung") });
                if (smartIntent == "iphone")
                    return Json(new { ok = true, reply = GetProductSmart(conn, "iphone") });

                var adv = GetAdvancedFilter(conn, msg);
                if (!string.IsNullOrEmpty(adv))
                    return Json(new { ok = true, reply = adv });

                var smartAI = SmartSearchV2(conn, msg);
                if (!string.IsNullOrEmpty(smartAI))
                    return Json(new { ok = true, reply = smartAI });

                // 👉 SMART SEARCH (để SAU)
                var keyword = NormalizeKeyword(msg);
                var smart = GetProductSmart(conn, keyword);
                if (!string.IsNullOrEmpty(smart) &&
                    !smart.Contains("chưa có") &&
                    !smart.Contains("Không tìm thấy"))
                {
                    return Json(new { ok = true, reply = smart });
                }

                var rec = RecommendL5(conn);
                if (!string.IsNullOrEmpty(rec))
                    return Json(new { ok = true, reply = rec });
                // 🤖 AI fallback 
                var aiReply = await CallAI("Bạn là nhân viên bán hàng thân thiện. Trả lời ngắn gọn: " + message);
                return Json(new { ok = true, reply = aiReply });
              
                // 👉 fallback
                
            }
            catch
            {
                return Json(new
                {
                    ok = true,
                    reply = "Server hơi mệt 😅 nhưng bạn thử hỏi lại nhé!"
                });
            }
        }

        // ================== DATA ==================

        private string GetProducts(string conn)
        {
            try
            {
                var list = new List<string>();

                using (var con = new SqlConnection(conn))
                {
                    con.Open();

                    var cmd = new SqlCommand("SELECT TOP 36 * FROM Products", con);
                    var rd = cmd.ExecuteReader();

                    while (rd.Read())
                    {
                        var name = "";
                        var price = 0m;

                        // 🔥 tự dò cột (không cần biết tên)
                        for (int i = 0; i < rd.FieldCount; i++)
                        {
                            var col = rd.GetName(i).ToLower();

                            if (col.Contains("name"))
                                name = rd[i].ToString();

                            if (col.Contains("price"))
                                price = rd[i] != DBNull.Value ? Convert.ToDecimal(rd[i]) : 0;
                        }

                        list.Add($"{name} - {price:N0}đ");
                    }
                }

                return list.Count > 0
                    ? "Shop đang có:\n" + string.Join("\n", list)
                    : "Không có sản phẩm 😅";
            }
            catch (Exception ex)
            {
                return "Lỗi sản phẩm: " + ex.Message;
            }
        }

        private string GetProductSmart(string conn, string message)
        {
            try
            {
                var keyword = NormalizeKeyword(message);
                var list = new List<string>();

                using (var con = new SqlConnection(conn))
                {
                    con.Open();
                    var cmd = new SqlCommand("SELECT TOP 36 * FROM Products", con);
                    var rd = cmd.ExecuteReader();

                    while (rd.Read())
                    {
                        string name = "";
                        decimal price = 0;

                        for (int i = 0; i < rd.FieldCount; i++)
                        {
                            var col = rd.GetName(i).ToLower();

                            if (col.Contains("name"))
                                name = rd[i].ToString();

                            if (col.Contains("price"))
                                price = rd[i] != DBNull.Value ? Convert.ToDecimal(rd[i]) : 0;
                        }

                        if (name.ToLower().Contains(keyword))
                        {
                            list.Add($"{name} - {price:N0}đ");
                        }
                    }
                }

                // 👉 nếu không tìm thấy → fallback thông minh
                if (list.Count == 0)
                {
                    return "Hiện chưa có sản phẩm phù hợp 😅\nBạn thử tìm iPhone, Samsung hoặc laptop nhé!";
                }

                return "Sản phẩm phù hợp:\n" + string.Join("\n", list);
            }
            catch (Exception ex)
            {
                return "Lỗi tìm kiếm: " + ex.Message;
            }
        }

        private string GetProductByName(string conn, string keyword)
        {
            try
            {
                var list = new List<string>();

                using (var con = new SqlConnection(conn))
                {
                    con.Open();

                    var cmd = new SqlCommand("SELECT TOP 5 * FROM Products", con);
                    var rd = cmd.ExecuteReader();

                    while (rd.Read())
                    {
                        string name = "";
                        decimal price = 0;

                        for (int i = 0; i < rd.FieldCount; i++)
                        {
                            var col = rd.GetName(i).ToLower();

                            if (col.Contains("name"))
                                name = rd[i].ToString();

                            if (col.Contains("price"))
                                price = rd[i] != DBNull.Value ? Convert.ToDecimal(rd[i]) : 0;
                        }

                        if (name.ToLower().Contains(keyword))
                        {
                            list.Add($"{name} - {price:N0}đ");
                        }
                    }
                }

                return list.Count > 0
                    ? "Sản phẩm phù hợp:\n" + string.Join("\n", list)
                    : "Không tìm thấy 😅";
            }
            catch (Exception ex)
            {
                return "Lỗi tìm sản phẩm: " + ex.Message;
            }
        }

        private string GetCheapProducts(string conn)
        {
            var list = new List<string>();

            using (var con = new SqlConnection(conn))
            {
                con.Open();
                var cmd = new SqlCommand("SELECT TOP 3 * FROM Products ORDER BY price ASC", con);
                var rd = cmd.ExecuteReader();

                while (rd.Read())
                {
                    string name = "";
                    decimal price = 0;

                    for (int i = 0; i < rd.FieldCount; i++)
                    {
                        var col = rd.GetName(i).ToLower();

                        if (col.Contains("name"))
                            name = rd[i].ToString();

                        if (col.Contains("price"))
                            price = rd[i] != DBNull.Value ? Convert.ToDecimal(rd[i]) : 0;
                    }

                    list.Add($"{name} chỉ {price:N0}đ 🔥");
                }
            }

            return "Mấy em giá mềm nè:\n" + string.Join("\n", list);
        }

        private string GetExpensiveProduct(string conn)
        {
            using (var con = new SqlConnection(conn))
            {
                con.Open();

                var cmd = new SqlCommand(@"
                SELECT TOP 3 product_name, price 
                FROM Products 
                ORDER BY price DESC", con);

                var rd = cmd.ExecuteReader();

                if (rd.Read())
                {
                    return $"Cao cấp nhất là {rd["product_name"]} giá {Convert.ToDecimal(rd["price"]):N0}đ 💎";
                }
            }

            return "Chưa có dữ liệu";
        }

        private string GetSpecs(string conn)
        {
            var list = new List<string>();

            using (var con = new SqlConnection(conn))
            {
                con.Open();
                var cmd = new SqlCommand(@"
                SELECT TOP 36 p.product_name, ps.spec_name, ps.spec_value
                FROM Product_Specifications ps
                JOIN Products p ON ps.product_id = p.product_id", con);

                var rd = cmd.ExecuteReader();

                while (rd.Read())
                {
                    list.Add($"{rd["product_name"]} - {rd["spec_name"]}: {rd["spec_value"]}");
                }
            }

            return "Thông số sản phẩm:\n" + string.Join("\n", list);
        }

        private string GetHotProducts(string conn)
        {
            var list = new List<string>();

            using (var con = new SqlConnection(conn))
            {
                con.Open();

                var cmd = new SqlCommand(@"
                SELECT TOP 5 product_name, price
                FROM Products
                ORDER BY price DESC", con);

                var rd = cmd.ExecuteReader();

                while (rd.Read())
                {
                    list.Add($"{rd["product_name"]} - {Convert.ToDecimal(rd["price"]):N0}đ 🔥");
                }
            }

            return "🔥 Sản phẩm hot hiện tại:\n" + string.Join("\n", list);
        }

        private string GetCategories(string conn)
        {
            try
            {
                var list = new List<string>();

                using (var con = new SqlConnection(conn))
                {
                    con.Open();
                    var cmd = new SqlCommand("SELECT category_name FROM Categories", con);
                    var rd = cmd.ExecuteReader();

                    while (rd.Read())
                    {
                        list.Add(rd[0].ToString());
                    }
                }

                return "Danh mục: " + string.Join(", ", list);
            }
            catch (Exception ex)
            {
                return "Lỗi danh mục: " + ex.Message;
            }
        }

        private string GetOrders(string conn)
        {
            using (var con = new SqlConnection(conn))
            {
                con.Open();
                var cmd = new SqlCommand("SELECT COUNT(*) FROM Orders", con);
                return $"Hiện có {(int)cmd.ExecuteScalar()} đơn hàng 📦";
            }
        }

        private string GetCustomers(string conn)
        {
            var list = new List<string>();

            using (var con = new SqlConnection(conn))
            {
                con.Open();

                var cmd = new SqlCommand(@"
                SELECT TOP 5 first_name + ' ' + last_name AS full_name 
                FROM Users", con);

                var rd = cmd.ExecuteReader();

                while (rd.Read())
                {
                    list.Add(rd["full_name"].ToString());
                }
            }

            return "Khách hàng: " + string.Join(", ", list);
        }

        // ================= AI =================

        private async Task<string> CallAI(string prompt)
        {
            var token = ConfigurationManager.AppSettings["chatBotAPIKey"];

                    var payload = new
                    {
                        model = "meta-llama/Meta-Llama-3-8B-Instruct",
                        messages = new[]
            {
                new {
                    role = "system",
                    content = "Bạn là nhân viên bán hàng điện thoại..."
                },
                new {
                    role = "user",
                    content = prompt
                }
            }
                    };

            var req = new HttpRequestMessage(
                HttpMethod.Post,
                "https://router.huggingface.co/v1/chat/completions"
            );

            req.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            req.Content = new StringContent(
                JsonConvert.SerializeObject(payload),
                Encoding.UTF8,
                "application/json"
            );

            var resp = await _http.SendAsync(req);
            var text = await resp.Content.ReadAsStringAsync();

            dynamic parsed = JsonConvert.DeserializeObject(text);
            return parsed.choices[0].message.content.ToString();
        }
        private string NormalizeKeyword(string msg)
        {
            msg = msg.ToLower();

            // bỏ từ rác
            var removeWords = new[] { "mua", "thêm", "cho", "tôi", "con", "cái", "đi", "shop" };

            foreach (var w in removeWords)
                msg = msg.Replace(w, "");

            return msg.Trim();
        }

        private string NormalizeText(string msg)
        {
            if (string.IsNullOrEmpty(msg)) return "";

            msg = msg.ToLower().Trim();

            return msg;
        }
        private List<string> ExtractBrands(string msg)
        {
            msg = NormalizeText(msg);

            var allBrands = new List<string>
    {
        "iphone","samsung","xiaomi","oppo","vivo","realme",
        "dell","hp","asus","lenovo","sony","canon","nikon","apple"
    };

            var result = new List<string>();

            foreach (var b in allBrands)
            {
                if (msg.Contains(b))
                    result.Add(b);

                // 👉 fuzzy nhẹ
                else if (msg.Replace(" ", "").Contains(b))
                    result.Add(b);
            }

            return result.Distinct().ToList();
        }

        private string GetBestSeller(string conn)
        {
            var list = new List<string>();

            try
            {
                using (var con = new SqlConnection(conn))
                {
                    con.Open();

                    var cmd = new SqlCommand(@"
                    SELECT TOP 5 p.product_name, SUM(oi.quantity) as total
                    FROM [Order Items] oi
                    JOIN Products p ON oi.product_id = p.product_id
                    GROUP BY p.product_name
                    ORDER BY total DESC", con);

                    var rd = cmd.ExecuteReader();

                    while (rd.Read())
                    {
                        list.Add($"{rd["product_name"]} 🔥 bán {rd["total"]}");
                    }
                }

                if (list.Count > 0)
                    return "🔥 Sản phẩm bán chạy nhất:\n" + string.Join("\n", list);

                // 👉 fallback thông minh
                return GetHotProducts(conn);
            }
            catch
            {
                return GetHotProducts(conn);
            }
        }

        private string GetNewest(string conn)
        {
            var list = new List<string>();

            using (var con = new SqlConnection(conn))
            {
                con.Open();

                var cmd = new SqlCommand(@"
                SELECT TOP 5 product_name, price
                FROM Products
                ORDER BY product_id DESC", con);

                var rd = cmd.ExecuteReader();

                while (rd.Read())
                {
                    list.Add($"{rd["product_name"]} - {Convert.ToDecimal(rd["price"]):N0}đ");
                }
            }

            return "Sản phẩm mới:\n" + string.Join("\n", list);
        }

        private string GetUnsold(string conn)
        {
            var list = new List<string>();

            try
            {
                using (var con = new SqlConnection(conn))
                {
                    con.Open();

                    var cmd = new SqlCommand(@"
                    SELECT product_name 
                    FROM Products
                    WHERE product_id NOT IN (
                        SELECT product_id FROM [Order Items]
                    )", con);

                    var rd = cmd.ExecuteReader();

                    while (rd.Read())
                    {
                        list.Add(rd["product_name"].ToString());
                    }
                }

                if (list.Count > 0)
                    return "😴 Mấy sản phẩm chưa bán được:\n" + string.Join("\n", list);

                // 👉 LEVEL 3: không có thì gợi ý
                return "🔥 Hiện tại sản phẩm nào cũng đã có đơn rồi 😎\nBạn muốn mình gợi ý sản phẩm hot không?";
            }
            catch
            {
                return GetHotProducts(conn); // 👉 fallback thông minh
            }
        }

        private string Safe(Func<string> func)
        {
            try
            {
                var result = func();
                return string.IsNullOrEmpty(result) ? "Không có dữ liệu 😅" : result;
            }
            catch
            {
                return "Dữ liệu đang lỗi 😅 nhưng bạn thử hỏi lại nhé!";
            }
        }

        private string GetByCategoryOrBrand(string conn, string msg)
        {
            var list = new List<string>();

            using (var con = new SqlConnection(conn))
            {
                con.Open();

                var cmd = new SqlCommand("SELECT TOP 36 product_name, price, brand FROM Products", con);
                var rd = cmd.ExecuteReader();

                while (rd.Read())
                {
                    var name = rd["product_name"].ToString().ToLower();
                    var brand = rd["brand"]?.ToString().ToLower();
                    var price = Convert.ToDecimal(rd["price"]);

                    if (msg.Contains("điện thoại") &&
                    (name.Contains("iphone") || name.Contains("samsung") || name.Contains("xiaomi") || name.Contains("oppo") || name.Contains("realme") || name.Contains("vivo")))
                    {
                        list.Add($"{rd["product_name"]} - {price:N0}đ");
                    }

                    else if (msg.Contains("laptop") && name.Contains("laptop"))
                        list.Add($"{rd["product_name"]} - {price:N0}đ");

                    else if (brand != null && msg.Contains(brand))
                        list.Add($"{rd["product_name"]} - {price:N0}đ");
                }
            }

            return list.Count > 0
                ? "Sản phẩm theo yêu cầu:\n" + string.Join("\n", list)
                : null;
        }

        private string GetAdvancedFilter(string conn, string msg)
        {
            var list = new List<string>();

            using (var con = new SqlConnection(conn))
            {
                con.Open();

                var cmd = new SqlCommand("SELECT product_name, price, brand FROM Products", con);
                var rd = cmd.ExecuteReader();

                while (rd.Read())
                {
                    var name = rd["product_name"].ToString().ToLower();
                    var brand = rd["brand"]?.ToString().ToLower();
                    var price = Convert.ToDecimal(rd["price"]);

                    // ===== FILTER HÃNG =====
                    if (brand != null && msg.Contains(brand))
                    {
                        list.Add($"{rd["product_name"]} - {price:N0}đ");
                        continue;
                    }

                    // ===== FILTER DANH MỤC =====
                    if (msg.Contains("điện thoại") &&
                        (name.Contains("iphone") || name.Contains("samsung") || name.Contains("xiaomi")))
                    {
                        list.Add($"{rd["product_name"]} - {price:N0}đ");
                        continue;
                    }

                    if (msg.Contains("laptop") && name.Contains("laptop"))
                    {
                        list.Add($"{rd["product_name"]} - {price:N0}đ");
                        continue;
                    }

                    // ===== FILTER GIÁ =====
                    if (msg.Contains("dưới 20") && price < 20000000)
                    {
                        list.Add($"{rd["product_name"]} - {price:N0}đ");
                        continue;
                    }

                    if (msg.Contains("trên 30") && price > 30000000)
                    {
                        list.Add($"{rd["product_name"]} - {price:N0}đ");
                        continue;
                    }
                }
            }

            return list.Count > 0
                ? "🎯 Sản phẩm phù hợp:\n" + string.Join("\n", list)
                : null;
        }

        private string DetectIntentSmart(string msg)
        {
            if (msg.Contains("mua") && msg.Contains("samsung"))
                return "samsung";

            if (msg.Contains("mua") && msg.Contains("iphone"))
                return "iphone";

            if (msg.Contains("pin trâu"))
                return "battery";

            return null;
        }

        private string DetectIntent(string msg)
        {
            if (msg.Contains("bán chạy") || msg.Contains("hot"))
                return "hot";

            if (msg.Contains("mới"))
                return "new";

            if (msg.Contains("không bán") || msg.Contains("chưa bán"))
                return "unsold";

            if (msg.Contains("rẻ"))
                return "cheap";

            if (msg.Contains("đắt"))
                return "expensive";

            if (msg.Contains("danh mục"))
                return "category";

            if (msg.Contains("đơn hàng"))
                return "order";

            if (msg.Contains("khách"))
                return "customer";

            if (msg.Contains("cấu hình") || msg.Contains("thông số"))
                return "spec";

            if (msg.Contains("thống kê"))
                return "stats";

            return null;

        }

        private string DetectIntentAI(string msg)
        {
            msg = NormalizeKeyword(msg);

            if (msg.Contains("so sánh") || msg.Contains("so sanh") || msg.Contains("vs"))
                return "compare";

            if (msg.Contains("mua") || msg.Contains("thêm"))
                return "buy";

            if (msg.Contains("giỏ"))
                return "cart";

            if (msg.Contains("gợi ý") || msg.Contains("goi y"))
                return "recommend";

            return "search";
        }

        private (string brand, decimal? maxPrice, bool pinTrau) AnalyzeMessage(string msg)
        {
            msg = msg.ToLower();

            // ===== BRAND =====
            string brand = null;
            if (msg.Contains("iphone")) brand = "iphone";
            else if (msg.Contains("samsung")) brand = "samsung";
            else if (msg.Contains("xiaomi")) brand = "xiaomi";

            // ===== PRICE =====
            decimal? price = null;

            var match = System.Text.RegularExpressions.Regex.Match(msg, @"\d+");
            if (match.Success)
            {
                var value = Convert.ToDecimal(match.Value);

                // 👉 hiểu "20tr", "20 triệu"
                if (msg.Contains("tr") || msg.Contains("triệu"))
                    price = value * 1000000;

                // 👉 hiểu "20000000"
                else if (value > 1000)
                    price = value;
            }

            // ===== PIN =====
            bool pinTrau = msg.Contains("pin trâu") || msg.Contains("pin lâu");

            return (brand, price, pinTrau);
        }

        private string SmartSearchV2(string conn, string msg)
        {
            var (brand, maxPrice, pinTrau) = AnalyzeMessage(msg);
            var list = new List<string>();

            using (var con = new SqlConnection(conn))
            {
                con.Open();
                var cmd = new SqlCommand("SELECT product_name, price FROM Products", con);
                var rd = cmd.ExecuteReader();

                while (rd.Read())
                {
                    var name = rd["product_name"].ToString().ToLower();
                    var price = Convert.ToDecimal(rd["price"]);

                    // 👉 chỉ gợi ý điện thoại
                    if (!(name.Contains("iphone") || name.Contains("samsung") || name.Contains("xiaomi")))
                        continue;

                    if (brand != null && !name.Contains(brand)) continue;
                    if (maxPrice != null && price > maxPrice) continue;

                    list.Add($"{rd["product_name"]} - {price:N0}đ");
                }
            }

            return list.Count > 0
                ? "🤖 Gợi ý chuẩn cho bạn:\n" + string.Join("\n", list.Take(5))
                : null;
        }

        private string CompareBrandsByCategory(string conn, string msg)
        {
            var dict = new Dictionary<string, List<decimal>>();

            // 👉 danh sách hãng hỗ trợ
            var allBrands = new List<string>
    {
        "iphone","samsung","xiaomi","oppo","vivo","realme",
        "dell","hp","asus","lenovo","sony","canon","nikon","apple","fujifilm"
    };

            // 👉 lấy hãng user nhập
            var brands = ExtractBrands(msg);

            if (brands.Count < 2)
                return "Bạn cần nhập 2 hãng để so sánh 😅";

            string category = null;
            if (msg.Contains("điện thoại")) category = "điện thoại";
            else if (msg.Contains("laptop")) category = "laptop";
            else if (msg.Contains("máy ảnh")) category = "máy ảnh";

            using (var con = new SqlConnection(conn))
            {
                con.Open();

                var cmd = new SqlCommand(@"
                SELECT p.product_name, p.price, p.brand, c.category_name
                FROM Products p
                JOIN Categories c ON p.category_id = c.category_id
                ", con);

                var rd = cmd.ExecuteReader();

                while (rd.Read())
                {
                    var cat = rd["category_name"].ToString().ToLower();
                    var brand = rd["brand"]?.ToString().ToLower();
                    var price = Convert.ToDecimal(rd["price"]);

                    if (brand == null) continue;

                    // 👉 chỉ lấy hãng user nhập
                    if (!brands.Contains(brand)) continue;

                    // 👉 lọc category nếu có
                    if (category != null && !cat.Contains(category))
                        continue;

                    if (!dict.ContainsKey(brand))
                        dict[brand] = new List<decimal>();

                    dict[brand].Add(price);
                }
            }

            if (dict.Count < 2)
                return "Không đủ dữ liệu để so sánh 😅";

            var result = $"⚖️ So sánh hãng ({category ?? "tất cả"}):\n\n";

            foreach (var item in dict)
            {
                var avg = item.Value.Average();
                result += $"- {item.Key.ToUpper()}: {avg:N0}đ (giá TB)\n";
            }

            var best = dict.OrderBy(x => x.Value.Average()).First();
            var expensive = dict.OrderByDescending(x => x.Value.Average()).First();

            result += $"\n💸 Rẻ nhất: {best.Key.ToUpper()}";
            result += $"\n💎 Cao cấp nhất: {expensive.Key.ToUpper()}";

            return result;
        }
        private string CompareProducts(string conn, string msg)
        {
            var allBrands = new List<string>
    {
        "iphone","samsung","xiaomi","oppo","vivo","realme",
        "dell","hp","asus","lenovo","sony","canon","nikon","apple","fujifilm"
    };

            var brands = ExtractBrands(msg);

            if (brands.Count < 2)
                return "Bạn cần nhập 2 hãng để so sánh 😅";

            var resultList = new List<(string name, decimal price, string brand)>();

            using (var con = new SqlConnection(conn))
            {
                con.Open();
                var cmd = new SqlCommand("SELECT product_name, price FROM Products", con);
                var rd = cmd.ExecuteReader();

                while (rd.Read())
                {
                    var name = rd["product_name"].ToString().ToLower();
                    var price = Convert.ToDecimal(rd["price"]);

                    foreach (var b in brands)
                    {
                        if (name.Contains(b))
                        {
                            resultList.Add((rd["product_name"].ToString(), price, b));
                            break;
                        }
                    }
                }
            }

            var result = "⚖️ So sánh:\n\n";

            foreach (var b in brands)
            {
                var top = resultList
                    .Where(x => x.brand == b)
                    .OrderByDescending(x => x.price)
                    .FirstOrDefault();

                if (!string.IsNullOrEmpty(top.name))
                    result += $"- {b.ToUpper()}: {top.name} ({top.price:N0}đ)\n";
            }

            // 👉 gợi ý theo hãng thực tế
            result += "\n👉 Gợi ý:\n";

            if (brands.Contains("iphone"))
                result += "• iPhone: mượt, ổn định, iOS\n";

            if (brands.Contains("oppo"))
                result += "• OPPO: camera đẹp, thiết kế đẹp\n";

            if (brands.Contains("vivo"))
                result += "• Vivo: selfie tốt, pin ổn\n";

            if (brands.Contains("realme"))
                result += "• Realme: hiệu năng cao, giá tốt\n";

            if (brands.Contains("samsung"))
                result += "• Samsung: màn đẹp, flagship mạnh\n";

            if (brands.Contains("apple"))
                result += "• Apple (MacBook): thiết kế đẹp, hiệu năng mạnh, hệ sinh thái tốt\n";

            if (brands.Contains("dell"))
                result += "• Dell: bền, ổn định, phù hợp học tập & doanh nghiệp\n";

            if (brands.Contains("asus"))
                result += "• ASUS: đa dạng, giá tốt, nhiều dòng gaming\n";

            if (brands.Contains("hp"))
                result += "• HP: thiết kế đẹp, mỏng nhẹ, văn phòng tốt\n";

            if (brands.Contains("lenovo"))
                result += "• Lenovo: hiệu năng ổn, giá hợp lý\n";

            if (brands.Contains("sony"))
                result += "• Sony: quay vlog, màu đẹp, nhỏ gọn\n";

            if (brands.Contains("nikon"))
                result += "• Nikon: chụp ảnh sắc nét, màu tự nhiên\n";

            if (brands.Contains("canon"))
                result += "• Canon: dễ dùng, màu da đẹp, hợp người mới\n";

            if (result.EndsWith("👉 Gợi ý:\n"))
            {
                result += "• Mỗi hãng có thế mạnh riêng, bạn muốn mình tư vấn chi tiết hơn không? 😊\n";
            }

            return result;
        }

        private string RecommendByBehavior(string conn, string msg)
        {
            if (msg.Contains("iphone"))
                return GetProductSmart(conn, "iphone");

            if (msg.Contains("samsung"))
                return GetProductSmart(conn, "samsung");

            return null;
        }

        private Dictionary<string, string> GetSpecsByProduct(SqlConnection con, int productId)
        {
            var dict = new Dictionary<string, string>();

            var cmd = new SqlCommand(@"
                SELECT spec_name, spec_value 
                FROM Product_Specifications 
                WHERE product_id = @id", con);

            cmd.Parameters.AddWithValue("@id", productId);

            var rd = cmd.ExecuteReader();

            while (rd.Read())
            {
                dict[rd["spec_name"].ToString().ToLower()] =
                    rd["spec_value"].ToString();
            }

            rd.Close();
            return dict;
        }
        private string RecommendL5(string conn)
        {
            var behavior = Session["behavior"] as List<string>;
            if (behavior == null || behavior.Count == 0) return null;

            var fav = behavior
                .GroupBy(x => x)
                .OrderByDescending(x => x.Count())
                .First().Key;

            var list = new List<string>();

            using (var con = new SqlConnection(conn))
            {
                con.Open();

                var cmd = new SqlCommand(@"
                SELECT TOP 5 product_name, price 
                FROM Products
                WHERE product_name LIKE @fav
                ORDER BY price DESC
                ", con);

                cmd.Parameters.AddWithValue("@fav", "%" + fav + "%");

                var rd = cmd.ExecuteReader();

                while (rd.Read())
                {
                    list.Add($"{rd["product_name"]} - {Convert.ToDecimal(rd["price"]):N0}đ");
                }
            }

            return list.Count > 0
                ? $"❤️ Dựa trên hành vi ({fav}), gợi ý cho bạn:\n" + string.Join("\n", list)
                : null;
        }
        private string AddToCart(string conn, string msg)
        {
            msg = msg.ToLower();

            var (brand, maxPrice, _) = AnalyzeMessage(msg);

            // 👉 lấy keyword sạch
            var keyword = NormalizeKeyword(msg);

            string productName = null;

            using (var con = new SqlConnection(conn))
            {
                con.Open();

                var query = @"
                SELECT TOP 1 product_name, price 
                FROM Products
                WHERE 1=1
                ";

                // ✅ dùng keyword thay vì msg
                if (!string.IsNullOrEmpty(keyword))
                    query += " AND LOWER(product_name) LIKE @kw";

                if (brand != null)
                    query += " AND LOWER(product_name) LIKE @brand";

                if (maxPrice != null)
                    query += " AND price <= @price";

                // 👉 logic chọn
                if (msg.Contains("rẻ"))
                    query += " ORDER BY price ASC";
                else
                    query += " ORDER BY price DESC";

                var cmd = new SqlCommand(query, con);

                if (!string.IsNullOrEmpty(keyword))
                    cmd.Parameters.AddWithValue("@kw", "%" + keyword + "%");

                if (brand != null)
                    cmd.Parameters.AddWithValue("@brand", "%" + brand + "%");

                if (maxPrice != null)
                    cmd.Parameters.AddWithValue("@price", maxPrice);

                var rd = cmd.ExecuteReader();

                if (rd.Read())
                    productName = rd["product_name"].ToString();
            }

            if (productName == null)
                return "Không tìm thấy sản phẩm phù hợp 😅";

            var cart = Session["cart"] as List<string> ?? new List<string>();
            cart.Add(productName);
            Session["cart"] = cart;

            return $"🛒 Đã thêm {productName} vào giỏ ({cart.Count} sản phẩm)";
        }
        private string ViewCart()
        {
            var cart = Session["cart"] as List<string>;

            if (cart == null || cart.Count == 0)
                return "🛒 Giỏ hàng đang trống";

            return "🛒 Giỏ hàng:\n" + string.Join("\n", cart);
        }
        private void TrackBehavior(string msg)
        {
            var list = Session["behavior"] as List<string> ?? new List<string>();

            if (msg.Contains("iphone")) list.Add("iphone");
            if (msg.Contains("samsung")) list.Add("samsung");
            if (msg.Contains("laptop")) list.Add("laptop");

            Session["behavior"] = list;
        }
        private string GetBestSellerSafe(string conn) => Safe(() => GetBestSeller(conn));
        private string GetNewestSafe(string conn) => Safe(() => GetNewest(conn));
        private string GetUnsoldSafe(string conn) => Safe(() => GetUnsold(conn));
        private string GetCategoriesSafe(string conn) => Safe(() => GetCategories(conn));
        private string GetOrdersSafe(string conn) => Safe(() => GetOrders(conn));
        private string GetCustomersSafe(string conn) => Safe(() => GetCustomers(conn));
        private string GetSpecsSafe(string conn) => Safe(() => GetSpecs(conn));
        private string GetCheapSafe(string conn) => Safe(() => GetCheapProducts(conn));
        private string GetExpensiveSafe(string conn) => Safe(() => GetExpensiveProduct(conn));
    }
}