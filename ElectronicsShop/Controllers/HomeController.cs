using ElectronicsShop.Models;
using ElectronicsShop.Helpers;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Web.Mvc;

namespace ElectronicsShop.Controllers
{
    public class HomeController : Customer_BaseController
    {
		private readonly string connectionString = ConnectionStrings.DefaultConnection;

		private ProductController productController = new ProductController();

        public ActionResult Index()
        {
            var bestSellingProducts = GetBestSellingProducts();
            var newProducts = GetNewProducts();

            ViewBag.BestSellingProducts = bestSellingProducts;
            ViewBag.NewProducts = newProducts;

            if (Session["UserId"] != null)
            {
                int userId = (int)Session["UserId"];
                ViewBag.WishlistIds = GetWishlistProductIds(userId);
            }

            return View();
        }

        public List<Product> GetBestSellingProducts()
        {
            var bestSellingProducts = new List<Product>();

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                string  query = @"
                                SELECT
                                    TOP 10 Products.product_id,
                                    SUM(Order_Items.quantity) AS TotalSold
                                FROM
                                    Products
                                JOIN 
                                    Order_Items ON Products.product_id = Order_Items.product_id
                                GROUP BY
                                    Products.product_id
                                ORDER BY
                                    TotalSold DESC;";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    con.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Product product = productController.GetProductByID((int)reader["product_id"]);
                            bestSellingProducts.Add(product);
                        }
                    }
                }
            }

            return bestSellingProducts;
        }

        private List<Product> GetNewProducts()
        {
            var newProducts = new List<Product>();

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                string query = @"
                    SELECT
                        TOP 10 Products.product_id
                    FROM
                        Products
                    WHERE
                        is_new = 1
                    ORDER BY
                        product_id DESC
                ";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    con.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Product product = productController.GetProductByID((int)reader["product_id"]);
                            newProducts.Add(product);
                        }
                    }
                }
            }

            return newProducts;
        }

        // Lấy danh sách wishlist cho user tương tự GetCartItemsByUserId ở CartController
        public List<Wishlist> GetWishListItemsByUserId(int userId)
        {
            var wishlists = new List<Wishlist>();

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                string query = @"
                    SELECT
                        w.*
                    FROM
                        Wishlist w
                        JOIN Products p ON w.product_id = p.product_id
                    WHERE
                        w.user_id = @UserId";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    con.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            wishlists.Add(new Wishlist
                            {
                                wishlist_id = (int)reader["wishlist_id"],
                                user_id = (int)reader["user_id"],
                                product_id = (int)reader["product_id"],
                                Product = productController.GetProductByID((int)reader["product_id"])
                            });
                        }
                    }
                }
            }
            return wishlists;
        }

        DataUtil data = new DataUtil();

        public ActionResult NewProduct()
        {
            var newest = data.GetNewestProduct();
            return View(newest);
        }

        public List<int> GetWishlistProductIds(int userId)
        {
            List<int> ids = new List<int>();

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                string query = "SELECT product_id FROM Wishlist WHERE user_id = @UserId";

                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@UserId", userId);

                con.Open();
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    ids.Add((int)reader["product_id"]);
                }
            }

            return ids;
        }
    }
}