using ElectronicsShop.Helpers;
using ElectronicsShop.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Web.Mvc;

namespace ElectronicsShop.Controllers
{
	public class WishlistController : Customer_BaseController
	{
		private readonly string connectionString = ConnectionStrings.DefaultConnection;

		// GET: Wishlist
		public ActionResult Index()
		{
			if (Session["UserId"] == null)
			{
				return RedirectToAction("Login", "Account");
			}

			int userId = (int)Session["UserId"];
			List<Wishlist> wishlists = GetWishListItemsByUserId(userId);
			return View(wishlists);
		}

        // POST: Add to Wishlist (supports AJAX)
        [HttpPost]
        public ActionResult Add(int productId)
        {
            if (Session["UserId"] == null)
            {
                if (Request.IsAjaxRequest())
                {
                    return Json(new { success = false, redirectUrl = Url.Action("Login", "Account") });
                }
                return RedirectToAction("Login", "Account");
            }

            int userId = (int)Session["UserId"];
            bool already = false;

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                con.Open();

                string checkQuery = @"
			SELECT COUNT(*)
			FROM Wishlist
			WHERE user_id = @UserId AND product_id = @ProductId";

                using (SqlCommand cmdChk = new SqlCommand(checkQuery, con))
                {
                    cmdChk.Parameters.AddWithValue("@UserId", userId);
                    cmdChk.Parameters.AddWithValue("@ProductId", productId);
                    int exists = (int)cmdChk.ExecuteScalar();
                    already = exists > 0;
                }

                if (!already)
                {
                    string insertQuery = @"
				INSERT INTO Wishlist (user_id, product_id)
				VALUES (@UserId, @ProductId)";

                    using (SqlCommand cmd = new SqlCommand(insertQuery, con))
                    {
                        cmd.Parameters.AddWithValue("@UserId", userId);
                        cmd.Parameters.AddWithValue("@ProductId", productId);
                        cmd.ExecuteNonQuery();
                    }
                }
            }

            // ✅ AJAX (giữ nguyên)
            if (Request.IsAjaxRequest())
            {
                var wishlistsNow = GetWishListItemsByUserId(userId);
                var html = RenderPartialViewToString("_WishlistItems", wishlistsNow);

                return Json(new
                {
                    success = true,
                    already = already,
                    wishlistCount = wishlistsNow.Count,
                    html = html
                });
            }

            // ✅ THÊM THÔNG BÁO
            TempData["Success"] = already
                ? "Sản phẩm đã có trong yêu thích!"
                : "Đã thêm vào yêu thích!";

            // ✅ QUAY LẠI TRANG PRODUCT
            return RedirectToAction("Details", "Product", new { id = productId });
        }

        // POST: Remove from Wishlist (supports AJAX)
        [HttpPost]
		public ActionResult Remove(int wishlistId)
		{
			if (Session["UserId"] == null)
			{
				if (Request.IsAjaxRequest())
				{
					return Json(new { success = false, redirectUrl = Url.Action("Login", "Account") });
				}
				return RedirectToAction("Login", "Account");
			}

			int userId = (int)Session["UserId"];
			int removedProductId = 0;

			using (SqlConnection con = new SqlConnection(connectionString))
			{
				con.Open();

				// Attempt to get product_id for the wishlist row (used to update icon on page)
				string selectQuery = @"
					SELECT product_id
					FROM Wishlist
					WHERE wishlist_id = @WishlistId AND user_id = @UserId";
				using (SqlCommand cmdSel = new SqlCommand(selectQuery, con))
				{
					cmdSel.Parameters.AddWithValue("@WishlistId", wishlistId);
					cmdSel.Parameters.AddWithValue("@UserId", userId);
					var obj = cmdSel.ExecuteScalar();
					if (obj != null && obj != DBNull.Value)
					{
						removedProductId = (int)obj;
					}
				}

				// Delete
				string query = @"
					DELETE FROM Wishlist
					WHERE wishlist_id = @WishlistId AND user_id = @UserId";
				using (SqlCommand cmd = new SqlCommand(query, con))
				{
					cmd.Parameters.AddWithValue("@WishlistId", wishlistId);
					cmd.Parameters.AddWithValue("@UserId", userId);
					cmd.ExecuteNonQuery();
				}
			}

			if (Request.IsAjaxRequest())
			{
				var wishlistsNow = GetWishListItemsByUserId(userId);
				var html = RenderPartialViewToString("_WishlistItems", wishlistsNow);
				return Json(new { success = true, wishlistCount = wishlistsNow.Count, html = html, removedProductId = removedProductId });
			}

            TempData["Success"] = "Đã xóa khỏi danh sách yêu thích!";
            return RedirectToAction("Index", "Wishlist");
        }

		// Helper: get wishlist items for header and pages
		public List<Wishlist> GetWishListItemsByUserId(int userId)
		{
			List<Wishlist> wishlists = new List<Wishlist>();

			using (SqlConnection con = new SqlConnection(connectionString))
			{
				string query = @"
                    SELECT
                        *
                    FROM
                        Wishlist w
                        JOIN Products p ON w.product_id = p.product_id
                    WHERE
                        w.user_id = @UserId";

				SqlCommand cmd = new SqlCommand(query, con);
				cmd.Parameters.AddWithValue("@UserId", userId);

				con.Open();
				SqlDataReader reader = cmd.ExecuteReader();

				while (reader.Read())
				{
					wishlists.Add(new Wishlist()
					{
						wishlist_id = (int)reader["wishlist_id"],
						user_id = (int)reader["user_id"],
						product_id = (int)reader["product_id"],
						Product = new ProductController().GetProductByID((int)reader["product_id"])
					});
				}
				reader.Close();
			}
			return wishlists;
		}

		// Render partial view to string (used for AJAX)
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
	}
}