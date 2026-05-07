using ElectronicsShop.Helpers;
using ElectronicsShop.Models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Web.Mvc;

namespace ElectronicsShop.Controllers
{

    public class ProductController : Customer_BaseController
    {
		private readonly string connectionString = ConnectionStrings.DefaultConnection;

		// GET: Product by ID
		public Product GetProductByID(int id)
        {
            
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                // Query to get product details
                string productQuery = @"
                    SELECT
                        *
                    FROM
                        Products p
                    WHERE
                        p.product_id = @ProductId";

                // Query to get product images
                string imagesQuery = @"
                    SELECT
                        *
                    FROM
                        Product_Images pi
                    WHERE
                        pi.product_id = @ProductId";

                // Query to get product images
                string categoryQuery = @"
                    SELECT
                        *
                    FROM
                        Categories category
                    WHERE
                        category.category_id = @CategoryID";

                SqlCommand cmd = new SqlCommand(productQuery, con);
                cmd.Parameters.AddWithValue("@ProductId", id);
                con.Open();
                SqlDataReader reader = cmd.ExecuteReader();

                Product product = null;
                if (reader.Read())
                {
                    product = new Product
                    {
                        product_id = (int)reader["product_id"],
                        product_name = reader["product_name"] as string ?? "",
                        description = reader["description"] as string ?? "",
                        price = reader["price"] != DBNull.Value ? (decimal?)reader["price"] : null,
                        discount_price = reader["discount_price"] != DBNull.Value ? (decimal?)reader["discount_price"] : null,
                        brand = reader["brand"] as string ?? "",
                        stock = reader["stock"] != DBNull.Value ? (int?)reader["stock"] : 0,
                        is_new = reader["is_new"] != DBNull.Value ? (bool?)reader["is_new"] : null,
                        category_id = (int)reader["category_id"],
                        Category = new Category(),
                        Product_Images = new List<Product_Images>(),
                        Specifications = new List<Product_Specification>()
                    };
                }

                reader.Close(); // Close the reader before executing the next command

                // Fetching product images
                if (product != null)
                {
                    SqlCommand imgCmd = new SqlCommand(imagesQuery, con);
                    imgCmd.Parameters.AddWithValue("@ProductId", id);
                    SqlDataReader imgReader = imgCmd.ExecuteReader();

                    while (imgReader.Read())
                    {
                        product.Product_Images.Add(new Product_Images
                        {
                            image_id = (int)imgReader["image_id"],
                            image_url = (string)imgReader["image_url"],
                            product_id = (int)imgReader["product_id"]
                        });
                    }
                    imgReader.Close();
                }


                // Fetching product images
                if (product != null)
                {
                    SqlCommand categoryCmd = new SqlCommand(categoryQuery, con);
                    categoryCmd.Parameters.AddWithValue("@CategoryID", product.category_id);
                    SqlDataReader categoryReader = categoryCmd.ExecuteReader();

                    while (categoryReader.Read())
                    {
                        product.Category = new Category
                        {
                            category_id = (int)categoryReader["category_id"],
                            category_name = (string)categoryReader["category_name"]
                        };
                    }
                    categoryReader.Close();
                }

                // Fetching product specfications
                // 🔥 LẤY THÔNG SỐ KỸ THUẬT
                if (product != null)
                {
                    string specQuery = @"
                    SELECT * 
                    FROM Product_Specifications 
                    WHERE product_id = @ProductId";

                    SqlCommand specCmd = new SqlCommand(specQuery, con);
                    specCmd.Parameters.AddWithValue("@ProductId", id);

                    SqlDataReader specReader = specCmd.ExecuteReader();

                    while (specReader.Read())
                    {
                        product.Specifications.Add(new Product_Specification
                        {
                            spec_id = (int)specReader["spec_id"],
                            product_id = (int)specReader["product_id"],
                            spec_group = specReader["spec_group"].ToString(),
                            spec_name = specReader["spec_name"].ToString(),
                            spec_value = specReader["spec_value"].ToString()
                        });
                    }

                    specReader.Close();
                }

                return product;
            }
        }


        // GET: Categories
        public ActionResult Categories()
        {
            List<Category> categories = new CategoriesController().GetCategories();
            return View(categories);
        }

        // GET: Products by Category
        public ActionResult ProductsByCategory(int categoryId, string brand, string type)
        {
            List<Product> products = new List<Product>();

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                string query = @"
        SELECT product_id 
        FROM Products
        WHERE category_id = @CategoryId";

                SqlCommand cmd = new SqlCommand();
                cmd.Connection = con;

                // lọc theo brand (KHÔNG phân biệt hoa thường)
                if (!string.IsNullOrEmpty(brand))
                {
                    query += " AND LOWER(brand) LIKE LOWER(@Brand)";
                    cmd.Parameters.AddWithValue("@Brand", "%" + brand + "%");
                }

                // lọc theo loại (tên sản phẩm)
                if (!string.IsNullOrEmpty(type))
                {
                    query += " AND LOWER(product_name) LIKE LOWER(@Type)";
                    cmd.Parameters.AddWithValue("@Type", "%" + type + "%");
                }

                query += " ORDER BY product_id DESC";

                cmd.CommandText = query;
                cmd.Parameters.AddWithValue("@CategoryId", categoryId);

                con.Open();
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    products.Add(GetProductByID((int)reader["product_id"]));
                }

                reader.Close(); // ✅ thêm cho chắc
            }

            // 🔥 QUAN TRỌNG NHẤT (THÊM ĐOẠN NÀY)
            if (Session["UserId"] != null)
            {
                int userId = (int)Session["UserId"];
                ViewBag.WishlistIds = GetWishlistProductIds(userId);
            }
            else
            {
                ViewBag.WishlistIds = new List<int>();
            }

            return View(products);
        }


        private List<Product> GetRelatedProducts(int currentProductId)
        {
            var relatedProducts = new List<Product>();

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                string query = @"
                    SELECT
                        TOP 10 p.product_id
                    FROM
                        Products p
                    WHERE
                        p.category_id = (
                            SELECT
                                category_id
                            FROM
                                Products
                            WHERE
                                product_id = @CurrentProductId
                        )
                        AND p.product_id != @CurrentProductId
                    ORDER BY
                        NEWID();
                ";


                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@CurrentProductId", currentProductId);
                    con.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Product product = GetProductByID((int)reader["product_id"]);
                            relatedProducts.Add(product);
                        }
                    }
                }
                
            }

            return relatedProducts;
        }


        // GET: Product Details
        public ActionResult Details(int? id)
        {
            if (id == null || id <= 0)
            {
                return RedirectToAction("Index", "Home");
            }

            Product product = GetProductByID(id.Value);

            if (product == null)
            {
                return HttpNotFound();
            }

            // 🔥 ADD 2 DÒNG QUAN TRỌNG NHẤT
            ViewBag.Reviews = GetReviewsByProductId(id.Value);
            ViewBag.RatingSummary = GetRatingSummary(id.Value);

            var relatedProducts = GetRelatedProducts(id.Value);
            ViewBag.RelatedProducts = relatedProducts;

            if (Session["UserId"] != null)
            {
                int userId = (int)Session["UserId"];
                ViewBag.WishlistIds = GetWishlistProductIds(userId);
            }
            else
            {
                ViewBag.WishlistIds = new List<int>();
            }

            return View(product);
        }


        // GET: Search Products
        public ActionResult Search(string query)
        {
            List<Product> products = new List<Product>();

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                string sql = @"
                    SELECT product_id FROM Products
                    WHERE LOWER(product_name) LIKE LOWER(@Query)
                       OR LOWER(brand) LIKE LOWER(@Query)";

                SqlCommand cmd = new SqlCommand(sql, con);
                cmd.Parameters.AddWithValue("@Query", "%" + query + "%");

                con.Open();
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    products.Add(GetProductByID((int)reader["product_id"]));
                }
            }

            ViewBag.Query = query;

            if (products.Count == 0)
            {
                ViewBag.Message = "Không tìm thấy sản phẩm 😢";
            }

            return View(products); // 🔥 DÒNG QUAN TRỌNG NHẤT
        }

        public List<string> GetBrands()
        {
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                string query = @"
                    SELECT
                        DISTINCT brand
                    FROM
                        Products
                    WHERE
                        brand IS NOT NULL;";

                SqlCommand cmd = new SqlCommand(query, con);
                con.Open();
                SqlDataReader reader = cmd.ExecuteReader();
                List<string> brands = new List<string>();
                while (reader.Read())
                {
                    brands.Add((string)reader["brand"]);
                }
                return brands;
            }
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

        public ActionResult HotDeals()
        {
            var products = new List<Product>();

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                        string query = @"
                SELECT p.*, pi.image_url
                FROM Products p
                LEFT JOIN Product_Images pi ON p.product_id = pi.product_id
                WHERE p.discount_price IS NOT NULL 
                      AND p.discount_price < p.price
                ORDER BY (p.price - p.discount_price) DESC";

                SqlCommand cmd = new SqlCommand(query, con);
                con.Open();

                SqlDataReader rd = cmd.ExecuteReader();

                var dict = new Dictionary<int, Product>();

                while (rd.Read())
                {
                    int id = (int)rd["product_id"];

                    if (!dict.ContainsKey(id))
                    {
                        var product = new Product
                        {
                            product_id = id,
                            product_name = rd["product_name"].ToString(),
                            price = rd["price"] as decimal?,
                            discount_price = rd["discount_price"] as decimal?,
                            Product_Images = new List<Product_Images>()
                        };

                        dict[id] = product;
                    }

                    // 👉 thêm ảnh nếu có
                    if (rd["image_url"] != DBNull.Value)
                    {
                        dict[id].Product_Images.Add(new Product_Images
                        {
                            image_url = rd["image_url"].ToString()
                        });
                    }
                }

                products = dict.Values.ToList();
            }

            // wishlist
            if (Session["UserId"] != null)
            {
                int userId = (int)Session["UserId"];
                ViewBag.WishlistIds = GetWishlistProductIds(userId);
            }
            else
            {
                ViewBag.WishlistIds = new List<int>();
            }

            return View(products);
        }

        public List<Review> GetReviewsByProductId(int productId)
        {
            List<Review> reviews = new List<Review>();

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                string query = @"
            SELECT r.*, u.first_name, u.last_name
            FROM Reviews r
            JOIN Users u ON r.user_id = u.user_id
            WHERE r.product_id = @ProductId
            ORDER BY r.created_at DESC";

                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@ProductId", productId);

                con.Open();
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    reviews.Add(new Review
                    {
                        review_id = (int)reader["review_id"],
                        product_id = (int)reader["product_id"],
                        user_id = (int)reader["user_id"],
                        rating = (int)reader["rating"],
                        comment = reader["comment"].ToString(),
                        created_at = (DateTime)reader["created_at"],
                        User = new User
                        {
                            first_name = reader["first_name"].ToString(),
                            last_name = reader["last_name"].ToString()
                        }
                    });
                }
            }

            return reviews;
        }

        [HttpPost]
        public ActionResult AddReview(int productId, int rating, string comment)
        {
            if (Session["UserId"] == null)
                return RedirectToAction("Login", "Account");

            int userId = (int)Session["UserId"];

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                con.Open();

                string insertQuery = @"
        INSERT INTO Reviews (product_id, user_id, rating, comment, created_at)
        VALUES (@ProductId, @UserId, @Rating, @Comment, GETDATE())";

                SqlCommand cmd = new SqlCommand(insertQuery, con);
                cmd.Parameters.AddWithValue("@ProductId", productId);
                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.Parameters.AddWithValue("@Rating", rating);
                cmd.Parameters.AddWithValue("@Comment", comment);

                cmd.ExecuteNonQuery();
            }

            TempData["Success"] = "Đánh giá thành công 👍";

            return RedirectToAction("Details", new { id = productId });
        }

        public RatingSummary GetRatingSummary(int productId)
        {
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                string query = @"
        SELECT 
            COUNT(*) as total,
            ISNULL(AVG(CAST(rating AS FLOAT)),0) as avgRating,
            ISNULL(SUM(CASE WHEN rating = 5 THEN 1 ELSE 0 END),0) as star5,
            ISNULL(SUM(CASE WHEN rating = 4 THEN 1 ELSE 0 END),0) as star4,
            ISNULL(SUM(CASE WHEN rating = 3 THEN 1 ELSE 0 END),0) as star3,
            ISNULL(SUM(CASE WHEN rating = 2 THEN 1 ELSE 0 END),0) as star2,
            ISNULL(SUM(CASE WHEN rating = 1 THEN 1 ELSE 0 END),0) as star1
        FROM Reviews
        WHERE product_id = @ProductId";

                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@ProductId", productId);

                con.Open();
                SqlDataReader rd = cmd.ExecuteReader();

                if (rd.Read())
                {
                    return new RatingSummary   // 🔥 KHÔNG dùng new { }
                    {
                        total = Convert.ToInt32(rd["total"]),
                        avg = Convert.ToDouble(rd["avgRating"]),
                        star5 = Convert.ToInt32(rd["star5"]),
                        star4 = Convert.ToInt32(rd["star4"]),
                        star3 = Convert.ToInt32(rd["star3"]),
                        star2 = Convert.ToInt32(rd["star2"]),
                        star1 = Convert.ToInt32(rd["star1"])
                    };
                }
            }

            // fallback
            return new RatingSummary
            {
                total = 0,
                avg = 0,
                star5 = 0,
                star4 = 0,
                star3 = 0,
                star2 = 0,
                star1 = 0
            };
        }
    }

}