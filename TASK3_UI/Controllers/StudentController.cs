using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Net.Http.Headers;
using System.Text.Json;
using TASK3_UI.Filters;
using TASK3_UI.Resources;
using TASK3_UI.Services.Interfaces;

namespace TASK3_UI.Controllers {
  [ServiceFilter(typeof(AuthFilter))]
  public class StudentController : Controller {
    private readonly ICrudService _crudService;
    private const string BaseUrl = "https://localhost:7040/api/Students";
    private const string GroupBaseUrl = "https://localhost:7040/api/Groups/whole";

    public StudentController(ICrudService crudService) {
      _crudService = crudService;
    }

    public async Task<IActionResult> Index(int page = 1) {
      var data = await _crudService.GetAllPaginatedAsync<StudentListItemGetResponse>(
          page,
          BaseUrl,
          new Dictionary<string, string> { { "pageSize", "4" } }
      );
      if (data.TotalPages != 0 && data.TotalPages < page)
        return RedirectToAction("Index", new { page = data.TotalPages });

      return View(data);
    }

    public async Task<IActionResult> Create() {
      await PopulateGroupsAsync();
      return View();
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromForm] StudentCreateRequest createRequest) {
      if (!ModelState.IsValid) {
        await PopulateGroupsAsync();
        return View(createRequest);
      }

      await _crudService.CreateAsync(createRequest, $"{BaseUrl}");
      return RedirectToAction("Index");
    }

    public async Task<IActionResult> Edit(int id) {
      var student = await _crudService.GetAsync<StudentCreateRequest>($"{BaseUrl}/{id}");
      await PopulateGroupsAsync();
      return View(student);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, [FromForm] StudentCreateRequest editRequest) {
      if (!ModelState.IsValid) {
        await PopulateGroupsAsync();
        return View(editRequest);
      }

      await _crudService.UpdateAsync(editRequest, $"{BaseUrl}/{id}");
      return RedirectToAction("Index");
    }

    public async Task<IActionResult> Delete(int id) {
      await _crudService.DeleteAsync($"{BaseUrl}/{id}");
      return RedirectToAction("Index");
    }

    private async Task PopulateGroupsAsync() {
      var groups = await _crudService.GetAsync<List<StudentCreateWithGroupRequest>>(GroupBaseUrl);
      ViewBag.Groups = new SelectList(groups, "Id", "Name");
    }
  }
}
