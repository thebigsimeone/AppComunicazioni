using AppComunicazioni.Data;
using AppComunicazioni.Models;
using AppComunicazioni.Models.DTO_s;
using AppComunicazioni.Interface;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AppComunicazioni.Controllers
{
    public class DestinatarisController : Controller
    {
        private readonly ComDbContext _context;
        private readonly IMapper _mapper;
        private readonly IRetryService _retryService;
        private readonly ILogger<DestinatarisController> _logger;

        public DestinatarisController(ComDbContext context, IMapper mapper, IRetryService retryService, ILogger<DestinatarisController> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _retryService = retryService ?? throw new ArgumentNullException(nameof(retryService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // GET: Destinataris
        public async Task<IActionResult> Index()
        {
            return await _retryService.ExecuteWithRetry(async () =>
            {
                var destinatariList = await _context.Destinataris.ToListAsync();
                if (destinatariList == null)
                {
                    return NotFound();
                }
                return View(destinatariList);
            }, _logger, this);
        }

        // GET: Destinataris/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (!id.HasValue)
            {
                return NotFound();
            }

            return await _retryService.ExecuteWithRetry(async () =>
            {
                var destinatari = await _context.Destinataris.FirstOrDefaultAsync(m => m.Id == id);
                if (destinatari == null)
                {
                    return NotFound();
                }

                var destinatariDTO = _mapper.Map<DestinatariDTO>(destinatari);
                return View(destinatariDTO);
            }, _logger, this);
        }

        // GET: Destinataris/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Destinataris/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Destinatario")] DestinatariDTO destinatariDTO)
        {
            if (ModelState.IsValid)
            {
                destinatariDTO.Monitor = Request.Form["Monitor"].Contains("true") ? "S" : "N";
                destinatariDTO.Attivo = Request.Form["Attivo"].Contains("true") ? "S" : "N";

                var destinatari = _mapper.Map<Destinatari>(destinatariDTO);

                return await _retryService.ExecuteWithRetry(async () =>
                {
                    using (var transaction = await _context.Database.BeginTransactionAsync())
                    {
                        try
                        {
                            await _context.Destinataris.AddAsync(destinatari);
                            await _context.SaveChangesAsync();
                            await transaction.CommitAsync();
                            return RedirectToAction(nameof(Index));
                        }
                        catch
                        {
                            await transaction.RollbackAsync();
                            throw;
                        }
                    }
                }, _logger, this);
            }
            return View(destinatariDTO);
        }

        // GET: Destinataris/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (!id.HasValue)
            {
                return NotFound();
            }

            return await _retryService.ExecuteWithRetry(async () =>
            {
                var destinatari = await _context.Destinataris.FindAsync(id);
                if (destinatari == null)
                {
                    return NotFound();
                }

                var destinatariDTO = _mapper.Map<DestinatariDTO>(destinatari);
                return View(destinatariDTO);
            }, _logger, this);
        }

        // POST: Destinataris/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Destinatario,Monitor,Attivo")] DestinatariDTO destinatariDTO)
        {
            if (id != destinatariDTO.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                destinatariDTO.Monitor = Request.Form.ContainsKey("Monitor") ? "S" : "N";
                destinatariDTO.Attivo = Request.Form.ContainsKey("Attivo") ? "S" : "N";

                return await _retryService.ExecuteWithRetry(async () =>
                {
                    using (var transaction = await _context.Database.BeginTransactionAsync())
                    {
                        try
                        {
                            var destinatari = _mapper.Map<Destinatari>(destinatariDTO);
                            _context.Update(destinatari);
                            await _context.SaveChangesAsync();
                            await transaction.CommitAsync();
                            return RedirectToAction(nameof(Index));
                        }
                        catch
                        {
                            await transaction.RollbackAsync();
                            throw;
                        }
                    }
                }, _logger, this);
            }
            return View(destinatariDTO);
        }

        // GET: Destinataris/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (!id.HasValue)
            {
                return NotFound();
            }

            return await _retryService.ExecuteWithRetry(async () =>
            {
                var destinatari = await _context.Destinataris.FirstOrDefaultAsync(m => m.Id == id);
                if (destinatari == null)
                {
                    return NotFound();
                }

                return View(destinatari);
            }, _logger, this);
        }

        // POST: Destinataris/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            return await _retryService.ExecuteWithRetry(async () =>
            {
                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        var destinatari = await _context.Destinataris.FindAsync(id);
                        if (destinatari != null)
                        {
                            _context.Destinataris.Remove(destinatari);
                            await _context.SaveChangesAsync();
                            await transaction.CommitAsync();
                        }
                        else
                        {
                            return NotFound();
                        }

                        return RedirectToAction(nameof(Index));
                    }
                    catch
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
                }
            }, _logger, this);
        }

        private bool DestinatariExists(int id)
        {
            return _context.Destinataris.Any(e => e.Id == id);
        }
    }
}
