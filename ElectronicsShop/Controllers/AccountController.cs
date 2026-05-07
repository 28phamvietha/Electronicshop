using ElectronicsShop.Models;
using ElectronicsShop.Helpers;
using System;
using System.Data.SqlClient;
using System.Web.Mvc;

namespace ElectronicsShop.Controllers
{
	public class AccountController : Customer_BaseController
	{
		// Database connection string
		private readonly string connectionString = ConnectionStrings.DefaultConnection;

		// GET: Register
		public ActionResult Register()
		{
			return View();
		}

		// POST: Register
		[HttpPost]
		public ActionResult Register(string firstName, string lastName, string email, string password, string phone, string confirm_password, string address)
		{
			using (SqlConnection con = new SqlConnection(connectionString))
			{
				string checkQuery = @"
                    SELECT
                        COUNT(*)
                    FROM
                        Users
                    WHERE
                        email = @Email
                        OR phone = @Phone";

				string insertQuery = @"
                    INSERT INTO
                        Users (first_name, last_name, email, password, phone, address, role)
                    VALUES
                        (@FirstName, @LastName, @Email, @Password, @Phone, @Address, 'customer')";

				try
				{
					SqlCommand checkCmd = new SqlCommand(checkQuery, con);
					checkCmd.Parameters.AddWithValue("@Email", email);
					checkCmd.Parameters.AddWithValue("@Phone", phone);

					con.Open();

					int existingUserCount = (int)checkCmd.ExecuteScalar();
					if (existingUserCount > 0)
					{
						ViewBag.MessageRegister = "Email hoặc số điện thoại đã tồn tại. Vui lòng thử lại.";
						return View();
					}

					SqlCommand cmd = new SqlCommand(insertQuery, con);
					cmd.Parameters.AddWithValue("@FirstName", firstName);
					cmd.Parameters.AddWithValue("@LastName", lastName);
					cmd.Parameters.AddWithValue("@Email", email);
					cmd.Parameters.AddWithValue("@Password", password);
					cmd.Parameters.AddWithValue("@Phone", phone);
                    cmd.Parameters.AddWithValue("@Address", address);

                    cmd.ExecuteNonQuery();

					ViewBag.MessageLogin = "Đăng ký tài khoản thành công!";
				}
				catch (Exception ex)
				{
					ViewBag.MessageRegister = "Thông tin đăng ký không hợp lệ: " + ex.Message;
				}
				finally
				{
					con.Close();
				}
			}
			return RedirectToAction("Login");
		}


		// GET: Login
		public ActionResult Login()
		{
			return View();
		}

		// POST: Login
		[HttpPost]
		public ActionResult Login(string email, string password)
		{
			using (SqlConnection con = new SqlConnection(connectionString))
			{
				string query = @"
                    SELECT
                        *
                    FROM
                        Users
                    WHERE
                        email = @Email
                        AND password = @Password";

				SqlCommand cmd = new SqlCommand(query, con);
				cmd.Parameters.AddWithValue("@Email", email);
				cmd.Parameters.AddWithValue("@Password", password);
				con.Open();
				SqlDataReader reader = cmd.ExecuteReader();
				if (reader.Read())
				{
					Session["UserId"] = reader["user_id"];
                    Session["UserName"] = reader["last_name"] + " " + reader["first_name"];
                    Session["Role"] = reader["role"];
					if ((string)reader["role"] == "admin")
					{
						return RedirectToAction("Statistical", "Admin_Orders");
					}
					return RedirectToAction("Index", "Home");
				}
				else
				{
					ViewBag.MessageLogin = "Thông tin đăng nhập không hợp lệ";
				}
			}
			return View();
		}

		// GET: Logout
		public ActionResult Logout()
		{
			Session.Clear();
			return RedirectToAction("Login");
		}


		public User GetUserById(int userId)
		{
			User user = null;
			using (SqlConnection con = new SqlConnection(connectionString))
			{
				string query = @"
                    SELECT
                        *
                    FROM
                        Users
                    WHERE
                        user_id = @UserId";

				SqlCommand cmd = new SqlCommand(query, con);
				cmd.Parameters.AddWithValue("@UserId", userId);

				con.Open();
				SqlDataReader reader = cmd.ExecuteReader();

				if (reader.Read())
				{
					user = new User()
					{
						first_name = (string)reader["first_name"],
						last_name = (string)reader["last_name"],
						email = (string)reader["email"],
						phone = (string)reader["phone"],
                        address = reader["address"]?.ToString()
                    };
				}
				reader.Close();
			}
			return user;
		}


		public ActionResult ShipmentManagement()
		{
			var roleObj = Session["Role"] ?? Session["role"];
			if (roleObj == null || roleObj.ToString() != "customer")
			{
				return RedirectToAction("Login", "Account");
			}

			if (Session["UserId"] == null)
			{
				return RedirectToAction("Login", "Account");
			}

			int userId;
			try
			{
				userId = Convert.ToInt32(Session["UserId"]);
			}
			catch
			{
				return RedirectToAction("Login", "Account");
			}

			ViewBag.User = GetUserById(userId);
			return View();
		}

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateInput(false)]
        public ActionResult UpdateProfile(string first_name, string last_name, string email, string phone, string address)
        {
            if (Session["UserId"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int userId;
            try
            {
                userId = Convert.ToInt32(Session["UserId"]);
            }
            catch
            {
                return RedirectToAction("Login", "Account");
            }

            // 🔥 FIX lỗi tiếng Việt HTML encode
            address = Server.HtmlDecode(address);

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                string updateQuery = @"
						UPDATE Users
						SET first_name = @FirstName,
							last_name = @LastName,
							email = @Email,
							phone = @Phone,
							address = @Address
						WHERE user_id = @UserId";

                try
                {
                    SqlCommand cmd = new SqlCommand(updateQuery, con);
                    cmd.Parameters.AddWithValue("@FirstName", first_name ?? string.Empty);
                    cmd.Parameters.AddWithValue("@LastName", last_name ?? string.Empty);
                    cmd.Parameters.AddWithValue("@Email", email ?? string.Empty);
                    cmd.Parameters.AddWithValue("@Phone", phone ?? string.Empty);
                    cmd.Parameters.AddWithValue("@Address", address ?? string.Empty);
                    cmd.Parameters.AddWithValue("@UserId", userId);

                    con.Open();
                    int rows = cmd.ExecuteNonQuery();

                    if (rows > 0)
                    {
                        Session["UserName"] = (last_name ?? "") + " " + (first_name ?? "");
                        TempData["ProfileMessage"] = "Cập nhật thông tin thành công.";
                    }
                    else
                    {
                        TempData["ProfileMessage"] = "Không có thay đổi.";
                    }
                }
                catch (Exception ex)
                {
                    TempData["ProfileMessage"] = "Lỗi: " + ex.Message;
                }
            }

            return RedirectToAction("ShipmentManagement");
        }
    }
}