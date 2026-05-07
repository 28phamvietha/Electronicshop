using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using ElectronicsShop.Models;

namespace ElectronicsShop.Controllers
{
    public class Product_ImagesController : Controller
    {
        private Db_ElectronicsShop db = new Db_ElectronicsShop();

        // GET: Product_Images
        public ActionResult Index()
        {
            var product_Images = db.Product_Images.Include(p => p.Product);
            return View(product_Images.ToList());
        }

        // GET: Product_Images/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Product_Images product_Images = db.Product_Images.Find(id);
            if (product_Images == null)
            {
                return HttpNotFound();
            }
            return View(product_Images);
        }

        // GET: Product_Images/Create
        public ActionResult Create()
        {
            ViewBag.product_id = new SelectList(db.Products, "product_id", "product_name");
            return View();
        }

        // POST: Product_Images/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "image_id,image_url,product_id")] Product_Images product_Images)
        {
            if (ModelState.IsValid)
            {
                db.Product_Images.Add(product_Images);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.product_id = new SelectList(db.Products, "product_id", "product_name", product_Images.product_id);
            return View(product_Images);
        }

        // GET: Product_Images/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Product_Images product_Images = db.Product_Images.Find(id);
            if (product_Images == null)
            {
                return HttpNotFound();
            }
            ViewBag.product_id = new SelectList(db.Products, "product_id", "product_name", product_Images.product_id);
            return View(product_Images);
        }

        // POST: Product_Images/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "image_id,image_url,product_id")] Product_Images product_Images)
        {
            if (ModelState.IsValid)
            {
                db.Entry(product_Images).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.product_id = new SelectList(db.Products, "product_id", "product_name", product_Images.product_id);
            return View(product_Images);
        }

        // GET: Product_Images/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Product_Images product_Images = db.Product_Images.Find(id);
            if (product_Images == null)
            {
                return HttpNotFound();
            }
            return View(product_Images);
        }

        // POST: Product_Images/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Product_Images product_Images = db.Product_Images.Find(id);
            db.Product_Images.Remove(product_Images);
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
