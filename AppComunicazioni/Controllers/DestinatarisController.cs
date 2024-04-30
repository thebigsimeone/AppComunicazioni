using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AppComunicazioni.Data;
using AppComunicazioni.Models;

namespace AppComunicazioni.Controllers
{
    public class DestinatarisController : Controller
    {
        private readonly ComDbContext _context;

        public DestinatarisController(ComDbContext context)
        {
            _context = context;
        }

        // GET: Destinataris
        public async Task<IActionResult> Index()
        {
            return View(await _context.Destinataris.ToListAsync());
        }

        // GET: Destinataris/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var destinatari = await _context.Destinataris
                .FirstOrDefaultAsync(m => m.Id == id);
            if (destinatari == null)
            {
                return NotFound();
            }

            return View(destinatari);
        }

        // GET: Destinataris/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Destinataris/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Destinatario")] Destinatari destinatari)
        {
            if (ModelState.IsValid)
            {
                _context.Add(destinatari);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(destinatari);
        }

        // GET: Destinataris/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var destinatari = await _context.Destinataris.FindAsync(id);
            if (destinatari == null)
            {
                return NotFound();
            }
            return View(destinatari);
        }

        // POST: Destinataris/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Destinatario")] Destinatari destinatari)
        {
            if (id != destinatari.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(destinatari);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DestinatariExists(destinatari.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(destinatari);
        }

        // GET: Destinataris/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var destinatari = await _context.Destinataris
                .FirstOrDefaultAsync(m => m.Id == id);
            if (destinatari == null)
            {
                return NotFound();
            }

            return View(destinatari);
        }

        // POST: Destinataris/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var destinatari = await _context.Destinataris.FindAsync(id);
            if (destinatari != null)
            {
                _context.Destinataris.Remove(destinatari);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool DestinatariExists(int id)
        {
            return _context.Destinataris.Any(e => e.Id == id);
        }
    }
}
