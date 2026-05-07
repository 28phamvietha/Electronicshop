using System.Collections.Generic;
using System.Web.Mvc;
using ElectronicsShop.Models;

namespace ElectronicsShop.Controllers
{
    public class Customer_BaseController : Controller
    {
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            base.OnActionExecuting(filterContext);
            ViewBag.Carts = GetCarts();
            ViewBag.Categories = GetCategories();
            ViewBag.Username = GetUsername();

            ViewBag.Query = "";
        }

        private Cart GetCarts()
        {
            return null;
        }

        private List<Category> GetCategories()
        {
            return new CategoriesController().GetCategories();
        }

        private string GetUsername()
        {
            return (string)Session["UserName"];
        }
    }
}