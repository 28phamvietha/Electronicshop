using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using ElectronicsShop.Models;

namespace ElectronicsShop.Controllers
{
    public class Admin_UsersController : Controller
    {
        private Db_ElectronicsShop db = new Db_ElectronicsShop();

        // GET: Users
        public ActionResult Index()
        {
            return View(db.Users.ToList());
        }

        // GET: Users/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            User user = db.Users.Find(id);
            if (user == null)
            {
                return HttpNotFound();
            }
            return View(user);
        }

        // GET: Users/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Users/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "user_id,first_name,last_name,email,password,phone,role")] User user)
        {
            if (ModelState.IsValid)
            {
                db.Users.Add(user);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(user);
        }

        // GET: Users/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            User user = db.Users.Find(id);
            if (user == null)
            {
                return HttpNotFound();
            }
            return View(user);
        }

        // POST: Users/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "user_id,first_name,last_name,email,password,phone,role")] User user)
        {
            if (ModelState.IsValid)
            {
                db.Entry(user).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(user);
        }

        // GET: Users/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            User user = db.Users.Find(id);
            if (user == null)
            {
                return HttpNotFound();
            }
            return View(user);
        }

        // POST: Users/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            var user = db.Users.Find(id);

            // 🔥 XÓA SHIPMENTS
            var shipments = db.Shipments.Where(s => s.user_id == id).ToList();
            db.Shipments.RemoveRange(shipments);

            // 🔥 LẤY ORDERS
            var orders = db.Orders.Where(o => o.user_id == id).ToList();

            foreach (var order in orders)
            {
                // 🔥 XÓA HẾT ORDER_ITEMS của từng order
                var items = db.Order_Items
                    .Where(x => x.order_id == order.order_id)
                    .ToList();

                db.Order_Items.RemoveRange(items);
            }

            db.SaveChanges(); // ✅ bắt buộc

            // 🔥 XÓA ORDERS
            db.Orders.RemoveRange(orders);
            db.SaveChanges();

            // 🔥 XÓA WISHLIST
            var wishlists = db.Wishlists.Where(x => x.user_id == id).ToList();
            db.Wishlists.RemoveRange(wishlists);

            // 🔥 XÓA CART
            var carts = db.Carts.Where(x => x.user_id == id).ToList();
            db.Carts.RemoveRange(carts);

            db.SaveChanges(); // ✅ gom lại

            // 🔥 XÓA USER
            db.Users.Remove(user);
            db.SaveChanges();

            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
