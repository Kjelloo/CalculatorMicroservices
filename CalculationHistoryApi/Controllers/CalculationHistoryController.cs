using CalculationHistoryApi.Data.Database;
using CalculationHistoryService.Data.Models;
using Microsoft.AspNetCore.Mvc;

namespace CalculationHistoryApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CalculationHistoryController : ControllerBase
    {
        private readonly IRepository<Calculation> _repository;

        public CalculationHistoryController(IRepository<Calculation> repository)
        {
            _repository = repository;
        }

        [HttpGet]
        public IEnumerable<Calculation> Get()
        {
            return _repository.Get();
        }
    }
}
