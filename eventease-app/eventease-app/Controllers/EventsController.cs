using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using eventease_app.Models;
using System.Security.Claims;

namespace eventease_app.Controllers
{
    [Authorize]
    public class EventsController : Controller
    {
        private readonly EventEaseContext _context;

        public EventsController(EventEaseContext context)
        {
            _context = context;
        }

        // GET: Events
        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            var events = await _context.Events
                                       .Include(e => e.Organizer)
                                       .Where(e => e.IsPublished)
                                       .OrderBy(e => e.StartDate)
                                       .ToListAsync();

            return View(events);
        }

        // GET: Events/Details/5
        [AllowAnonymous]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var @event = await _context.Events
                .Include(e => e.Organizer)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (@event == null)
                return NotFound();

            return View(@event);
        }

        // GET: Events/Create
        [Authorize(Roles = "organizer,admin")]
        public IActionResult Create()
        {
            if (User.IsInRole("organizer"))
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
                var organizer = _context.Users.FirstOrDefault(u => u.Id == userId);

                if (organizer == null || !organizer.Approved)
                    return RedirectToAction("AccessDenied", "Home");
            }

            ViewData["OrganizerId"] = new SelectList(_context.Users.Where(u => u.Role == "organizer"), "Id", "Email");
            return View();
        }

        // POST: Events/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "organizer,admin")]
        public async Task<IActionResult> Create([Bind("OrganizerId,Theme,Location,Description,LongDescription,Category,ThumbnailFileName,HeroFileName,StartDate,Price,IsPublished")] Event @event)
        {
            if (ModelState.IsValid)
            {
                @event.CreatedAt = DateTime.UtcNow;
                @event.UpdatedAt = DateTime.UtcNow;

                _context.Add(@event);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["OrganizerId"] = new SelectList(_context.Users, "Id", "Id", @event.OrganizerId);
            return View(@event);
        }

        // GET: Events/Edit/5
        [Authorize(Roles = "organizer,admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var @event = await _context.Events.FindAsync(id);
            if (@event == null)
                return NotFound();

            ViewData["OrganizerId"] = new SelectList(_context.Users, "Id", "Id", @event.OrganizerId);
            return View(@event);
        }

        // POST: Events/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "organizer,admin")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,OrganizerId,Theme,Location,Description,LongDescription,Category,ThumbnailFileName,HeroFileName,StartDate,Price,IsPublished,CreatedAt,UpdatedAt")] Event @event)
        {
            if (id != @event.Id)
                return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    @event.UpdatedAt = DateTime.UtcNow;
                    _context.Update(@event);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!EventExists(@event.Id))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["OrganizerId"] = new SelectList(_context.Users, "Id", "Id", @event.OrganizerId);
            return View(@event);
        }

        // GET: Events/Delete/5
        [Authorize(Roles = "organizer,admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var @event = await _context.Events
                .Include(e => e.Organizer)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (@event == null)
                return NotFound();

            return View(@event);
        }

        // POST: Events/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "organizer,admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var @event = await _context.Events.FindAsync(id);
            if (@event != null)
                _context.Events.Remove(@event);

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool EventExists(int id)
        {
            return _context.Events.Any(e => e.Id == id);
        }
    }
}
