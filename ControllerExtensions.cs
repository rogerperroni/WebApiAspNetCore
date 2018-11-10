using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;

namespace Extensions
{
    public static class ControllerExtensions
    {
       
        public static IActionResult OkOrNotFound<T>(this Controller controller, IEnumerable<T> result)
        {
            if (result == null || !result.Any())
                return controller.NotFound();
            return controller.Ok(result);
        }
        public static IActionResult OkOrNotFound<T>(this Controller controller, T result)
        {
            if (result == null)
                return controller.NotFound();
            return controller.Ok(result);
        }
    }
}
