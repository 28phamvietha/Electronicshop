using ElectronicsShop.Helpers;
using ElectronicsShop.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Web.Mvc;

namespace ElectronicsShop.Controllers
{
	public class CartController : Customer_BaseController
	{
		private readonly string connectionString = ConnectionStrings.DefaultConnection;

		// GET: Cart
		public ActionResult Index()
		{
			if (Session["UserId"] == null)
			{
				return RedirectToAction("Login", "Account");
			}

			int userId = (int)Session["UserId"];

			List<Cart> carts = GetCartItemsByUserId(userId);
			return View(carts);
		}

        // POST: Add to Cart (server form)

        public JsonResult AddToCart(int productId, int quantity)
        {
            if (Session["UserId"] == null)
            {
                return Json(new { success = false, redirectUrl = Url.Action("Login", "Account") });
            }

            int userId = (int)Session["UserId"];

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                con.Open();

                if (quantity < 1)
                {
                    return Json(new { success = false, message = "Số lượng phải lớn hơn 0" });
                }

                if (!IsStockAvailable(con, productId, quantity))
                {
                    return Json(new { success = false, message = "Vượt quá số lượng trong kho." });
                }

                int? currentCartQuantity = GetCurrentCartQuantity(con, userId, productId);

                if (currentCartQuantity.HasValue)
                {
                    int newQuantity = currentCartQuantity.Value + quantity;

                    if (newQuantity > GetProductStock(con, productId))
                    {
                        return Json(new { success = false, message = "Vượt quá số lượng trong kho." });
                    }

                    UpdateCart(con, userId, productId, newQuantity);
                }
                else
                {
                    InsertIntoCart(con, userId, productId, quantity);
                }
            }

            return Json(new { success = true, message = "Đã thêm vào giỏ hàng!" });
        }

        // POST: Add to Cart (AJAX-compatible)
        [HttpPost]
		public ActionResult Add(int productId, int quantity = 1)
		{
            if (productId <= 0)
            {
                TempData["Error"] = "Sản phẩm không hợp lệ!";
                return RedirectToAction("Index", "Home");
            }

            if (Session["UserId"] == null)
			{
				if (Request.IsAjaxRequest())
				{
					return Json(new { success = false, redirectUrl = Url.Action("Login", "Account") });
				}
				return RedirectToAction("Login", "Account");
			}

			int userId = (int)Session["UserId"];
			bool stockOk = true;
			using (SqlConnection con = new SqlConnection(connectionString))
			{
				con.Open();

				if (!IsStockAvailable(con, productId, quantity))
				{
					stockOk = false;
				}
				else
				{
					int? currentCartQuantity = GetCurrentCartQuantity(con, userId, productId);

					if (currentCartQuantity.HasValue)
					{
						int newQuantity = currentCartQuantity.Value + quantity;
						if (newQuantity > GetProductStock(con, productId))
						{
							stockOk = false;
						}
						else
						{
							UpdateCart(con, userId, productId, newQuantity);
						}
					}
					else
					{
						InsertIntoCart(con, userId, productId, quantity);
					}
				}
			}

			if (Request.IsAjaxRequest())
			{
				if (!stockOk)
				{
					return Json(new { success = false, message = "Vượt quá số lượng trong kho." });
				}
				var cartsNow = GetCartItemsByUserId(userId);
				var html = RenderPartialViewToString("_CartItems", cartsNow);
				int quantityCount = (int)cartsNow.Sum(c => c.quantity);
				int rowCount = cartsNow.Count;
				decimal subtotal = (decimal)cartsNow.Sum(c => (c.quantity * (c.Product?.discount_price ?? c.Product?.price ?? 0M)));
				return Json(new { success = true, quantityCount = quantityCount, rowCount = rowCount, cartSubtotal = subtotal, html = html });
			}

			return RedirectToAction("Index", "Home");
		}

		private bool IsStockAvailable(SqlConnection con, int productId, int requestedQuantity)
		{
			int currentStock = GetProductStock(con, productId);
			return currentStock >= requestedQuantity;
		}

        private int GetProductStock(SqlConnection con, int productId)
        {
            string query = @"SELECT stock FROM Products WHERE product_id = @ProductId";
            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                cmd.Parameters.AddWithValue("@ProductId", productId);

                object result = cmd.ExecuteScalar();

                return result != DBNull.Value && result != null ? (int)result : 0;
            }
        }

        private int? GetCurrentCartQuantity(SqlConnection con, int userId, int productId)
		{
			string query = @"
                SELECT
                    quantity
                FROM
                    Cart
                WHERE
                    user_id = @UserId
                    AND product_id = @ProductId";

			using (SqlCommand cmd = new SqlCommand(query, con))
			{
				cmd.Parameters.AddWithValue("@UserId", userId);
				cmd.Parameters.AddWithValue("@ProductId", productId);

				object result = cmd.ExecuteScalar();
				return result != null ? (int?)result : null;
			}
		}

		private void UpdateCart(SqlConnection con, int userId, int productId, int quantity)
		{
			string query = @"
                UPDATE
                    Cart
                SET
                    quantity = @Quantity
                WHERE
                    user_id = @UserId
                    AND product_id = @ProductId";

			using (SqlCommand cmd = new SqlCommand(query, con))
			{
				cmd.Parameters.AddWithValue("@Quantity", quantity);
				cmd.Parameters.AddWithValue("@UserId", userId);
				cmd.Parameters.AddWithValue("@ProductId", productId);

				cmd.ExecuteNonQuery();
			}
		}

		private void InsertIntoCart(SqlConnection con, int userId, int productId, int quantity)
		{
			string query = @"
                INSERT INTO
                    Cart (user_id, product_id, quantity)
                VALUES
                    (@UserId, @ProductId, @Quantity)";

			using (SqlCommand cmd = new SqlCommand(query, con))
			{
				cmd.Parameters.AddWithValue("@UserId", userId);
				cmd.Parameters.AddWithValue("@ProductId", productId);
				cmd.Parameters.AddWithValue("@Quantity", quantity);

				cmd.ExecuteNonQuery();
			}
		}

		// POST: Remove from Cart (supports AJAX)
		[HttpPost]
		public ActionResult Remove(int cartId)
		{
			if (Session["UserId"] == null)
			{
				if (Request.IsAjaxRequest())
				{
					return Json(new { success = false, redirectUrl = Url.Action("Login", "Account") });
				}
				return RedirectToAction("Login", "Account");
			}

			int removedProductId = 0;
			int userId = (int)Session["UserId"];

			using (SqlConnection con = new SqlConnection(connectionString))
			{
				con.Open();

				// attempt to read product_id for UI update
				string selectQuery = @"
                    SELECT product_id, user_id
                    FROM Cart
                    WHERE cart_id = @CartId";
				using (SqlCommand cmdSel = new SqlCommand(selectQuery, con))
				{
					cmdSel.Parameters.AddWithValue("@CartId", cartId);
					using (var rdr = cmdSel.ExecuteReader())
					{
						if (rdr.Read())
						{
							removedProductId = rdr["product_id"] == DBNull.Value ? 0 : (int)rdr["product_id"];
							// ensure the cart row belongs to current user; if not, prevent delete below
							userId = rdr["user_id"] == DBNull.Value ? userId : (int)rdr["user_id"];
						}
					}
				}

				// Delete
				string query = @"
                    DELETE FROM
                        Cart
                    WHERE
                        cart_id = @CartId";
				using (SqlCommand cmd = new SqlCommand(query, con))
				{
					cmd.Parameters.AddWithValue("@CartId", cartId);
					cmd.ExecuteNonQuery();
				}
			}

			if (Request.IsAjaxRequest())
			{
				var cartsNow = GetCartItemsByUserId((int)Session["UserId"]);
				var html = RenderPartialViewToString("_CartItems", cartsNow);
				int quantityCount = (int)cartsNow.Sum(c => c.quantity);
				int rowCount = cartsNow.Count;
				decimal subtotal = (decimal)cartsNow.Sum(c => (c.quantity * (c.Product?.discount_price ?? c.Product?.price ?? 0M)));
                return Json(new
                {
                    success = true,
                    message = "Đã xoá sản phẩm!",
                    quantityCount = quantityCount,
                    rowCount = rowCount,
                    cartSubtotal = subtotal,
                    html = html,
                    removedProductId = removedProductId
                });
            }

			return RedirectToAction("Index", "Home");
		}

		public List<Cart> GetCartItemsByUserId(int userId)
		{
			List<Cart> carts = new List<Cart>();

			using (SqlConnection con = new SqlConnection(connectionString))
			{
				string query = @"
                    SELECT
                        *
                    FROM
                        Cart c
                        JOIN Products p ON c.product_id = p.product_id
                    WHERE
                        c.user_id = @UserId";

				SqlCommand cmd = new SqlCommand(query, con);
				cmd.Parameters.AddWithValue("@UserId", userId);

				con.Open();
				SqlDataReader reader = cmd.ExecuteReader();

				while (reader.Read())
				{
					carts.Add(new Cart()
					{
						cart_id = (int)reader["cart_id"],
						user_id = (int)reader["user_id"],
						product_id = (int)reader["product_id"],
						quantity = (int)reader["quantity"],
						Product = new ProductController().GetProductByID((int)reader["product_id"])
					});
				}
				reader.Close();
			}
			return carts;
		}

		public ActionResult Purchase()
		{
			if (Session["UserId"] == null)
			{
				return RedirectToAction("Login", "Account");
			}

			int userId = (int)Session["UserId"];
			User user = new AccountController().GetUserById(userId);

			if (user != null)
			{
				user.Shipments = new ShipmentsController().GetShipmentsByUserId(userId);
				user.Carts = GetCartItemsByUserId(userId);
				if (user.Shipments == null || !user.Shipments.Any())
				{
					user.Shipments = new List<Shipment>
					{
						new Shipment
						{
							shipment_address = "",
							shipment_city = "",
							shipment_country = "",
							shipment_zip_code = ""
						}
					};
				}
			}

			return View(user);
		}

        // POST: Purchase
        [HttpPost]
        public ActionResult Purchase(string recipient_first_name,string recipient_last_name,string recipient_phone,string address,string province,string ward,string zipCode,string paymentMethod)
        {
            if (Session["UserId"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int userId = (int)Session["UserId"];

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                con.Open();

                // 🔥 GỘP ĐỊA CHỈ FULL
                string fullAddress = (address ?? "") + ", " + (ward ?? "") + ", " + (province ?? "");

                // ✅ INSERT SHIPMENT (FIX FULL LỖI SQL Ở ĐÂY)
                string queryShipment = @"
            INSERT INTO Shipments (
                user_id,
                recipient_first_name,
                recipient_last_name,
                recipient_phone,
                shipment_address,
                shipment_city,
                shipment_country,
                shipment_zip_code
            )
            OUTPUT INSERTED.shipment_id
            VALUES (
                @UserId,
                @FirstName,
                @LastName,
                @Phone,
                @Address,
                @City,
                @Ward,
                @ZipCode
            )";

                SqlCommand cmdShipment = new SqlCommand(queryShipment, con);

                cmdShipment.Parameters.AddWithValue("@UserId", userId);
                cmdShipment.Parameters.AddWithValue("@FirstName", recipient_first_name ?? "");
                cmdShipment.Parameters.AddWithValue("@LastName", recipient_last_name ?? "");
                cmdShipment.Parameters.AddWithValue("@Phone", recipient_phone ?? "");

                cmdShipment.Parameters.AddWithValue("@Address", fullAddress); // FULL địa chỉ
                cmdShipment.Parameters.AddWithValue("@City", province ?? ""); // tỉnh
                cmdShipment.Parameters.AddWithValue("@Ward", ward ?? "");     // xã
                cmdShipment.Parameters.AddWithValue("@ZipCode", zipCode ?? "");

                int shipmentId = (int)cmdShipment.ExecuteScalar();

                // 🔥 TÍNH TỔNG TIỀN
                decimal totalAmount = Convert.ToDecimal(CalculateTotalAmount(userId, con));

                // 🔥 TRẠNG THÁI ĐƠN HÀNG
                string status = (paymentMethod != null && paymentMethod.ToLower() == "vnpay")
                    ? "PendingPayment"
                    : "Processing";

                // ✅ INSERT ORDER
                string queryOrder = @"
            INSERT INTO Orders (
                shipment_id,
                user_id,
                order_date,
                total_amount,
                status,
                payment_method
            )
            OUTPUT INSERTED.order_id
            VALUES (
                @ShipmentId,
                @UserId,
                @OrderDate,
                @TotalAmount,
                @Status,
                @PaymentMethod
            )";

                SqlCommand cmdOrder = new SqlCommand(queryOrder, con);

                cmdOrder.Parameters.AddWithValue("@ShipmentId", shipmentId);
                cmdOrder.Parameters.AddWithValue("@UserId", userId);
                cmdOrder.Parameters.AddWithValue("@OrderDate", DateTime.Now);
                cmdOrder.Parameters.AddWithValue("@TotalAmount", totalAmount);
                cmdOrder.Parameters.AddWithValue("@Status", status);
                cmdOrder.Parameters.AddWithValue("@PaymentMethod", paymentMethod ?? "");

                int orderId = (int)cmdOrder.ExecuteScalar();

                // ✅ INSERT ORDER ITEMS
                string queryOrderItems = @"
            INSERT INTO Order_Items (order_id, product_id, quantity, price)
            SELECT
                @OrderId,
                c.product_id,
                c.quantity,
                ISNULL(p.discount_price, p.price)
            FROM Cart c
            JOIN Products p ON c.product_id = p.product_id
            WHERE c.user_id = @UserId";

                SqlCommand cmdOrderItems = new SqlCommand(queryOrderItems, con);
                cmdOrderItems.Parameters.AddWithValue("@OrderId", orderId);
                cmdOrderItems.Parameters.AddWithValue("@UserId", userId);
                cmdOrderItems.ExecuteNonQuery();

                // 🔥 VNPAY → KHÔNG XÓA CART
                if (paymentMethod != null && paymentMethod.ToLower() == "vnpay")
                {
                    return RedirectToAction("VNPayDemo", new { orderId = orderId });
                }

                // ✅ TRỪ KHO
                string queryUpdateStock = @"
            UPDATE p
            SET p.stock = p.stock - c.quantity
            FROM Products p
            JOIN Cart c ON p.product_id = c.product_id
            WHERE c.user_id = @UserId";

                SqlCommand cmdUpdateStock = new SqlCommand(queryUpdateStock, con);
                cmdUpdateStock.Parameters.AddWithValue("@UserId", userId);
                cmdUpdateStock.ExecuteNonQuery();

                // ✅ XÓA GIỎ HÀNG
                string queryClearCart = "DELETE FROM Cart WHERE user_id = @UserId";

                SqlCommand cmdClearCart = new SqlCommand(queryClearCart, con);
                cmdClearCart.Parameters.AddWithValue("@UserId", userId);
                cmdClearCart.ExecuteNonQuery();
            }

            return RedirectToAction("OrderHistory");
        }

        // GET: Order History
        public ActionResult OrderHistory()
        {
            if (Session["UserId"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int userId = (int)Session["UserId"];

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                string query = @"
            SELECT
                o.order_id,
                o.order_date,
                ISNULL(SUM(oi.quantity * oi.price), 0) AS total_amount,
                o.status,
                ISNULL(s.shipment_address, '') AS shipment_address,
                COUNT(oi.product_id) AS NumberOfProducts
            FROM Orders o
            JOIN Shipments s ON o.shipment_id = s.shipment_id
            LEFT JOIN Order_Items oi ON o.order_id = oi.order_id
            WHERE o.user_id = @UserId
            GROUP BY o.order_id, o.order_date, o.status, s.shipment_address
            ORDER BY o.order_date DESC";

                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@UserId", userId);

                con.Open();
                SqlDataReader reader = cmd.ExecuteReader();

                List<Order> orders = new List<Order>();
                Dictionary<int, string> shipmentAddresses = new Dictionary<int, string>();
                Dictionary<int, int> productCounts = new Dictionary<int, int>();

                while (reader.Read())
                {
                    int orderId = (int)reader["order_id"];

                    orders.Add(new Order
                    {
                        order_id = orderId,
                        order_date = (DateTime)reader["order_date"],
                        total_amount = Convert.ToDecimal(reader["total_amount"]),
                        status = reader["status"]?.ToString()
                    });

                    shipmentAddresses[orderId] = reader["shipment_address"]?.ToString() ?? "---";
                    productCounts[orderId] = Convert.ToInt32(reader["NumberOfProducts"]);
                }

                ViewBag.ShipmentAddresses = shipmentAddresses;
                ViewBag.ProductCounts = productCounts;

                return View(orders);
            }
        }

        // GET: Order Details
        public ActionResult OrderDetails(int id)
		{
			if (Session["UserId"] == null)
			{
				return RedirectToAction("Login", "Account");
			}
			using (SqlConnection con = new SqlConnection(connectionString))
			{
                string query = @"
					SELECT
						oi.order_item_id,
						oi.quantity,
						oi.price,
						oi.product_id,
						o.order_id,          
						o.order_date,        
						o.user_id,
						o.shipment_id,
						o.status
					FROM
						Order_Items oi
						JOIN Products p ON oi.product_id = p.product_id
						JOIN Orders o ON oi.order_id = o.order_id
					WHERE
						oi.order_id = @OrderId";

                SqlCommand cmd = new SqlCommand(query, con);
				cmd.Parameters.AddWithValue("@OrderId", id);
				con.Open();
				SqlDataReader reader = cmd.ExecuteReader();
				List<Order_Items> orderDetails = new List<Order_Items>();
				while (reader.Read())
					orderDetails.Add(new Order_Items
					{
						order_item_id = (int)reader["order_item_id"],
						quantity = (int)reader["quantity"],
						price = (decimal)reader["price"],
						Order = new Order
						{
                            order_id = (int)reader["order_id"],            
                            order_date = (DateTime)reader["order_date"],
                            status = (string)reader["status"],
							User = new AccountController().GetUserById((int)reader["user_id"]),
							Shipment = new ShipmentsController().GetShipmentById((int)reader["shipment_id"])
						},
						Product = new ProductController().GetProductByID((int)reader["product_id"])
					});
				return View(orderDetails);
			}
		}

        private decimal CalculateTotalAmount(int userId, SqlConnection con)
        {
            string query = @"
        SELECT
            SUM(c.quantity * ISNULL(p.discount_price, p.price))
        FROM
            Cart c
            JOIN Products p ON c.product_id = p.product_id
        WHERE
            c.user_id = @UserId";

            SqlCommand cmd = new SqlCommand(query, con);
            cmd.Parameters.AddWithValue("@UserId", userId);

            object result = cmd.ExecuteScalar();
            return result != DBNull.Value && result != null ? (decimal)result : 0;
        }

        public ActionResult VNPay(decimal totalAmount, int orderId)
        {
            string vnp_Url = ConfigurationManager.AppSettings["VNPAY:BaseUrl"];
            string vnp_TmnCode = ConfigurationManager.AppSettings["VNPAY:TmnCode"];
            string vnp_HashSecret = ConfigurationManager.AppSettings["VNPAY:HashSecret"];
            string vnp_ReturnUrl = ConfigurationManager.AppSettings["VNPAY:CallbackUrl"];

            var vnPay = new VnPayLibrary();

            vnPay.AddRequestData("vnp_Version", "2.1.0");
            vnPay.AddRequestData("vnp_Command", "pay");
            vnPay.AddRequestData("vnp_TmnCode", vnp_TmnCode);

            long amount = (long)(totalAmount * 100);
            vnPay.AddRequestData("vnp_Amount", amount.ToString());

            vnPay.AddRequestData("vnp_CurrCode", "VND");
            vnPay.AddRequestData("vnp_TxnRef", orderId.ToString());
            vnPay.AddRequestData("vnp_OrderInfo", $"Thanh toán đơn hàng #{orderId}");
            vnPay.AddRequestData("vnp_OrderType", "other");
            vnPay.AddRequestData("vnp_Locale", "vn");
            vnPay.AddRequestData("vnp_ReturnUrl", vnp_ReturnUrl);
            vnPay.AddRequestData("vnp_IpAddr", Utils.GetIpAddress());

            // ⏰ thời gian tạo
            vnPay.AddRequestData("vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss"));

            // 🔥 thời gian hết hạn (15 phút)
            vnPay.AddRequestData("vnp_ExpireDate", DateTime.Now.AddMinutes(15).ToString("yyyyMMddHHmmss"));

            vnPay.AddRequestData("vnp_SecureHashType", "HmacSHA512");

            string paymentUrl = vnPay.CreateRequestUrl(vnp_Url, vnp_HashSecret);

            return Redirect(paymentUrl);
        }

        
        [HttpGet]
        public ActionResult ConfirmPayment()
        {
            string hashSecret = ConfigurationManager.AppSettings["VNPAY:HashSecret"];
            var vnPay = new VnPayLibrary();

            foreach (string key in Request.QueryString)
            {
                if (!string.IsNullOrEmpty(key) && key.StartsWith("vnp_"))
                {
                    vnPay.AddResponseData(key, Request.QueryString[key]);
                }
            }

            string secureHash = Request.QueryString["vnp_SecureHash"];
            bool checkSignature = vnPay.ValidateSignature(secureHash, hashSecret);

            if (!checkSignature)
            {
                TempData["msg"] = "Sai chữ ký!";
                return RedirectToAction("OrderHistory");
            }

            int orderId = int.Parse(vnPay.GetResponseData("vnp_TxnRef"));
            string responseCode = vnPay.GetResponseData("vnp_ResponseCode");

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                con.Open();

                // 🔥 LẤY USER ID
                string getUser = "SELECT user_id FROM Orders WHERE order_id = @OrderId";
                int userId = (int)new SqlCommand(getUser, con)
                {
                    Parameters = { new SqlParameter("@OrderId", orderId) }
                }.ExecuteScalar();

                if (responseCode == "00")
                {
                    // ✅ update status
                    string updateOrder = @"
                UPDATE Orders
                SET status = 'Processing', payment_method = 'vnpay'
                WHERE order_id = @OrderId";

                    new SqlCommand(updateOrder, con)
                    {
                        Parameters = { new SqlParameter("@OrderId", orderId) }
                    }.ExecuteNonQuery();

                    // ✅ trừ kho
                    string updateStock = @"
                UPDATE p
                SET p.stock = p.stock - oi.quantity
                FROM Products p
                JOIN Order_Items oi ON p.product_id = oi.product_id
                WHERE oi.order_id = @OrderId";

                    new SqlCommand(updateStock, con)
                    {
                        Parameters = { new SqlParameter("@OrderId", orderId) }
                    }.ExecuteNonQuery();

                    // ✅ xoá cart (QUAN TRỌNG NHẤT)
                    string clearCart = "DELETE FROM Cart WHERE user_id = @UserId";

                    new SqlCommand(clearCart, con)
                    {
                        Parameters = { new SqlParameter("@UserId", userId) }
                    }.ExecuteNonQuery();

                    TempData["msg"] = "Thanh toán thành công";
                }
                else
                {
                    // ❌ fail
                    string fail = "UPDATE Orders SET status = 'Failed' WHERE order_id = @OrderId";

                    new SqlCommand(fail, con)
                    {
                        Parameters = { new SqlParameter("@OrderId", orderId) }
                    }.ExecuteNonQuery();

                    TempData["msg"] = "Thanh toán thất bại";
                }
            }

            return RedirectToAction("OrderHistory");
        }

        private string RenderPartialViewToString(string viewName, object model)
		{
			ViewData.Model = model;
			using (var sw = new StringWriter())
			{
				var viewResult = ViewEngines.Engines.FindPartialView(ControllerContext, viewName);
				var viewContext = new ViewContext(ControllerContext, viewResult.View, ViewData, TempData, sw);
				viewResult.View.Render(viewContext, sw);
				viewResult.ViewEngine.ReleaseView(ControllerContext, viewResult.View);
				return sw.GetStringBuilder().ToString();
			}
		}

        [HttpPost]
        public ActionResult UpdateQuantity(int cartId, int quantity)
        {
            if (Session["UserId"] == null)
            {
                return Json(new { success = false });
            }

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                con.Open();

                string query = @"UPDATE Cart 
                         SET quantity = @Quantity 
                         WHERE cart_id = @CartId";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@Quantity", quantity);
                    cmd.Parameters.AddWithValue("@CartId", cartId);
                    cmd.ExecuteNonQuery();
                }
            }

            // load lại cart
            var carts = GetCartItemsByUserId((int)Session["UserId"]);

            int quantityCount = carts.Sum(c => c.quantity ?? 0);

            decimal subtotal = carts.Sum(c => (c.quantity ?? 0) * (c.Product.discount_price ?? c.Product.price ?? 0));

            return Json(new
            {
                success = true,
                quantityCount = quantityCount,
                subtotal = subtotal
            });
        }

        public ActionResult VNPayDemo(int? orderId)
        {
            if (orderId == null)
            {
                return RedirectToAction("Purchase");
            }

            ViewBag.OrderId = orderId;

            // 🔥 thời gian hết hạn (15 phút)
            ViewBag.ExpireTime = DateTime.Now.AddMinutes(15).ToString("o"); // ISO format

            return View();
        }

        [HttpPost]
        public ActionResult VNPayDemoConfirm(int orderId)
        {
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                con.Open();

                // lấy userId
                string getUser = "SELECT user_id FROM Orders WHERE order_id = @OrderId";
                int userId = (int)new SqlCommand(getUser, con)
                {
                    Parameters = { new SqlParameter("@OrderId", orderId) }
                }.ExecuteScalar();

                // ✅ update status
                string updateOrder = "UPDATE Orders SET status = 'Processing' WHERE order_id = @OrderId";
                new SqlCommand(updateOrder, con)
                {
                    Parameters = { new SqlParameter("@OrderId", orderId) }
                }.ExecuteNonQuery();

                // ✅ trừ kho
                string updateStock = @"
        UPDATE p
        SET p.stock = p.stock - oi.quantity
        FROM Products p
        JOIN Order_Items oi ON p.product_id = oi.product_id
        WHERE oi.order_id = @OrderId";

                new SqlCommand(updateStock, con)
                {
                    Parameters = { new SqlParameter("@OrderId", orderId) }
                }.ExecuteNonQuery();

                // ✅ xoá cart
                string clearCart = "DELETE FROM Cart WHERE user_id = @UserId";

                new SqlCommand(clearCart, con)
                {
                    Parameters = { new SqlParameter("@UserId", userId) }
                }.ExecuteNonQuery();
            }

            TempData["msg"] = "Thanh toán thành công (demo)";
            return RedirectToAction("OrderHistory");
        }

        [HttpPost]
        public ActionResult VNPayDemoFail(int orderId)
        {
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                con.Open();

                // ❌ xoá order chưa thanh toán
                string deleteOrder = @"
				DELETE FROM Orders
				WHERE order_id = @OrderId AND status = 'PendingPayment'";

                new SqlCommand(deleteOrder, con)
                {
                    Parameters = { new SqlParameter("@OrderId", orderId) }
                }.ExecuteNonQuery();
            }

            TempData["msg"] = "Bạn đã huỷ thanh toán";
            return RedirectToAction("Purchase");
        }

        public ActionResult VNPayReturn()
        {
            var responseCode = Request.QueryString["vnp_ResponseCode"];
            var orderId = Convert.ToInt32(Request.QueryString["vnp_TxnRef"]);

            if (responseCode == "00")
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();

                    // ✅ LẤY USER ID TỪ ORDER (QUAN TRỌNG)
                    string getUser = "SELECT user_id FROM Orders WHERE order_id = @OrderId";
                    SqlCommand cmdGetUser = new SqlCommand(getUser, con);
                    cmdGetUser.Parameters.AddWithValue("@OrderId", orderId);

                    int userId = (int)cmdGetUser.ExecuteScalar();

                    // ✅ UPDATE STATUS
                    string updateOrder = "UPDATE Orders SET status = 'Processing' WHERE order_id = @OrderId";
                    SqlCommand cmd = new SqlCommand(updateOrder, con);
                    cmd.Parameters.AddWithValue("@OrderId", orderId);
                    cmd.ExecuteNonQuery();

                    // ✅ TRỪ KHO
                    string updateStock = @"
                UPDATE p
                SET p.stock = p.stock - oi.quantity
                FROM Products p
                JOIN Order_Items oi ON p.product_id = oi.product_id
                WHERE oi.order_id = @OrderId";

                    SqlCommand cmdStock = new SqlCommand(updateStock, con);
                    cmdStock.Parameters.AddWithValue("@OrderId", orderId);
                    cmdStock.ExecuteNonQuery();

                    // ✅ XÓA GIỎ HÀNG (GIỜ MỚI CHẮC CHẮN CHẠY)
                    string clearCart = "DELETE FROM Cart WHERE user_id = @UserId";
                    SqlCommand cmdCart = new SqlCommand(clearCart, con);
                    cmdCart.Parameters.AddWithValue("@UserId", userId);
                    cmdCart.ExecuteNonQuery();
                }

                return RedirectToAction("OrderHistory");
            }

            return Content("Thanh toán thất bại ❌");
        }
    }
}