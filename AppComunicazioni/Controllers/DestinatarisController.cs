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
        private readonly IEncryptionService _encryptionService; // Aggiunto servizio crittografia

        public DestinatarisController(ComDbContext context, IMapper mapper, IRetryService retryService, ILogger<DestinatarisController> logger, IEncryptionService encryptionService)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _retryService = retryService ?? throw new ArgumentNullException(nameof(retryService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _encryptionService = encryptionService ?? throw new ArgumentNullException(nameof(encryptionService));
        }

        // GET: Destinataris
        public async Task<IActionResult> Index()
        {
            return await _retryService.ExecuteWithRetry(async () =>
            {
                var destinatariList = await _context.Destinataris.ToListAsync();
                return View(destinatariList);
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
        public async Task<IActionResult> Create([Bind("Destinatario,Monitor,Attivo,Report")] DestinatariDTO destinatariDTO)
        {
            if (ModelState.IsValid)
            {
                destinatariDTO.Monitor = Request.Form.ContainsKey("Monitor") ? "S" : "N";
                destinatariDTO.Attivo = Request.Form.ContainsKey("Attivo") ? "S" : "N";
                destinatariDTO.Report = Request.Form.ContainsKey("Report") ? "S" : "N";

                var destinatari = _mapper.Map<Destinatari>(destinatariDTO);

                return await _retryService.ExecuteWithRetry(async () =>
                {
                    await _context.Destinataris.AddAsync(destinatari);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }, _logger, this);
            }
            return View(destinatariDTO);
        }

        // GET: Destinataris/Details/{id}
        public async Task<IActionResult> Details(string id)
        {
            int decryptedId = DecryptId(id);
            if (decryptedId == -1) return BadRequest("ID non valido.");

            return await _retryService.ExecuteWithRetry(async () =>
            {
                var destinatari = await _context.Destinataris.FirstOrDefaultAsync(m => m.Id == decryptedId);
                if (destinatari == null) return NotFound();

                var destinatariDTO = _mapper.Map<DestinatariDTO>(destinatari);
                return View(destinatariDTO);
            }, _logger, this);
        }

        // GET: Destinataris/Edit/{id}
        public async Task<IActionResult> Edit(string id)
        {
            int decryptedId = DecryptId(id);
            if (decryptedId == -1) return BadRequest("ID non valido.");

            return await _retryService.ExecuteWithRetry(async () =>
            {
                var destinatari = await _context.Destinataris.FindAsync(decryptedId);
                if (destinatari == null) return NotFound();

                var destinatariDTO = _mapper.Map<DestinatariDTO>(destinatari);

                // Popola ViewData con l'ID crittografato
                ViewData["EncryptedId"] = _encryptionService.Encrypt(decryptedId.ToString());

                return View(destinatariDTO);
            }, _logger, this);
        }

        // POST: Destinataris/Edit/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string EncryptedId, [Bind("Destinatario,Monitor,Attivo,Report")] DestinatariDTO destinatariDTO)
        {
            int decryptedId = DecryptId(EncryptedId);
            if (decryptedId == -1) return BadRequest("ID non valido.");

            if (ModelState.IsValid)
            {
                destinatariDTO.Monitor = Request.Form.ContainsKey("Monitor") ? "S" : "N";
                destinatariDTO.Attivo = Request.Form.ContainsKey("Attivo") ? "S" : "N";
                destinatariDTO.Report = Request.Form.ContainsKey("Report") ? "S" : "N";

                return await _retryService.ExecuteWithRetry(async () =>
                {
                    var destinatariToUpdate = await _context.Destinataris.FindAsync(decryptedId);
                    if (destinatariToUpdate == null) return NotFound();

                    // Aggiorna solo le proprietà specifiche
                    destinatariToUpdate.Destinatario = destinatariDTO.Destinatario;
                    destinatariToUpdate.Monitor = destinatariDTO.Monitor;
                    destinatariToUpdate.Attivo = destinatariDTO.Attivo;

                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }, _logger, this);
            }
            return View(destinatariDTO);
        }

        // GET: Destinataris/Delete/{id}
        public async Task<IActionResult> Delete(string id)
        {
            int decryptedId = DecryptId(id);
            if (decryptedId == -1) return BadRequest("ID non valido.");

            return await _retryService.ExecuteWithRetry(async () =>
            {
                var destinatari = await _context.Destinataris.FirstOrDefaultAsync(m => m.Id == decryptedId);
                if (destinatari == null) return NotFound();

                return View(destinatari);
            }, _logger, this);
        }

        // POST: Destinataris/Delete/{id}
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            int decryptedId = DecryptId(id);
            if (decryptedId == -1) return BadRequest("ID non valido.");

            return await _retryService.ExecuteWithRetry(async () =>
            {
                var destinatari = await _context.Destinataris.FindAsync(decryptedId);
                if (destinatari != null)
                {
                    _context.Destinataris.Remove(destinatari);
                    await _context.SaveChangesAsync();
                }
                return RedirectToAction(nameof(Index));
            }, _logger, this);
        }

        private int DecryptId(string encryptedId)
        {
            try
            {
                return int.Parse(_encryptionService.Decrypt(encryptedId));
            }
            catch
            {
                return -1; // ID non valido
            }
        }
    }
}
