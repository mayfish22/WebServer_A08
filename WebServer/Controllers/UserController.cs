using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebServer.Extensions;
using WebServer.Models;
using WebServer.Models.WebServerDB;
using WebServer.Services;
using Microsoft.EntityFrameworkCore;

namespace WebServer.Controllers
{
    [Authorize]
    [ServiceFilter(typeof(WebServer.Filters.AuthorizeFilter))]
    public class UserController : Controller
    {
        private readonly WebServerDBContext _WebServerDBContext;
        private readonly SiteService _SiteService;

        public UserController(WebServerDBContext WebServerDBContext
            , SiteService SiteService)
        {
            _WebServerDBContext = WebServerDBContext;
            _SiteService = SiteService;
        }

        #region Index
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            await Task.Yield();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> GetColumns()
        {
            try
            {
                var _columnList = new List<string>
                {
                    nameof(UserIndexViewModel.Account),
                    nameof(UserIndexViewModel.Name),
                    nameof(UserIndexViewModel.Birthday),
                    nameof(UserIndexViewModel.Email),
                };
                var columns = await _SiteService.GetDatatableColumns<UserIndexViewModel>(_columnList);

                return new SystemTextJsonResult(new
                {
                    status = "success",
                    data = columns,
                });
            }
            catch (Exception e)
            {
                return new SystemTextJsonResult(new
                {
                    status = "fail",
                    message = e.Message,
                });
            }
        }

        /// <summary>
        /// For Data Table
        /// </summary>
        /// <param name="draw">DataTable用,不用管他</param>
        /// <param name="start">起始筆數</param>
        /// <param name="length">顯示筆數</param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> GetData(int draw, int start, int length)
        {
            await Task.Yield();
            try
            {
                //總筆數
                int nTotalCount = await _WebServerDBContext.User.CountAsync();

                var info = from n1 in _WebServerDBContext.User
                           select new UserIndexViewModel
                           {
                               ID = n1.ID,
                               Account = n1.Account,
                               Name = n1.Name,
                               Birthday = n1.Birthday,
                               Email = n1.Email,
                           };

                #region 關鍵字搜尋
                if (!string.IsNullOrEmpty((string)Request.Form["search[value]"]))
                {
                    string sQuery = Request.Form["search[value]"].ToString().ToUpper();
                    bool IsNumber = decimal.TryParse(sQuery, out decimal nQuery);
                    info = info.Where(t =>
                                 (!string.IsNullOrEmpty(t.Account) && t.Account.ToUpper().Contains(sQuery))
                                || (!string.IsNullOrEmpty(t.Name) && t.Name.ToUpper().Contains(sQuery))
                                || (!string.IsNullOrEmpty(t.Birthday) && t.Birthday.ToUpper().Contains(sQuery))
                                || (!string.IsNullOrEmpty(t.Email) && t.Email.ToUpper().Contains(sQuery))
                    );
                }
                #endregion 關鍵字搜尋

                #region 排序
                int sortColumnIndex = (string)Request.Form["order[0][column]"] == null ? -1 : int.Parse(Request.Form["order[0][column]"]);
                string sortDirection = (string)Request.Form["order[0][dir]"] == null ? "" : Request.Form["order[0][dir]"].ToString().ToUpper();
                string sortColumn = Request.Form["columns[" + sortColumnIndex + "][data]"].ToString() ?? "";

                bool bDescending = sortDirection.Equals("DESC");
                switch (sortColumn)
                {
                    case nameof(UserIndexViewModel.Account):
                        info = bDescending ? info.OrderByDescending(o => o.Account) : info.OrderBy(o => o.Account);
                        break;

                    case nameof(UserIndexViewModel.Name):
                        info = bDescending ? info.OrderByDescending(o => o.Name) : info.OrderBy(o => o.Name);
                        break;
                    case nameof(UserIndexViewModel.Birthday):
                        info = bDescending ? info.OrderByDescending(o => o.Birthday) : info.OrderBy(o => o.Birthday);
                        break;
                    case nameof(UserIndexViewModel.Email):
                        info = bDescending ? info.OrderByDescending(o => o.Email) : info.OrderBy(o => o.Email);
                        break;

                    default:
                        info = info.OrderBy(o => o.Account);
                        break;
                }

                #endregion 排序

                //結果
                var list = nTotalCount == 0 ? new List<UserIndexViewModel>() : info.Skip(start).Take(Math.Min(length, nTotalCount - start)).ToList();

                return new SystemTextJsonResult(new DataTableData
                {
                    Draw = draw,
                    Data = list,
                    RecordsTotal = nTotalCount,
                    RecordsFiltered = info.Count()
                });
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }
        #endregion
    }
}