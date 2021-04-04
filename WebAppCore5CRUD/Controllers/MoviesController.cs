using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NToastNotify;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WebAppCore5CRUD.Models;
using WebAppCore5CRUD.ViewModels;

namespace WebAppCore5CRUD.Controllers
{  
    public class MoviesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IToastNotification _toastNotification;
        private List<string> allowExtentions = new List<string> { ".jpg", ".png" };
        private long maxAllowedPosterSize = 1048576;

        public MoviesController(ApplicationDbContext context, IToastNotification toastNotification)
        {
            this._context = context;
            this._toastNotification = toastNotification;
        }

        public async Task<IActionResult> Index()
        {
            var movies = await _context.Movies.OrderByDescending(m => m.Rate).ToListAsync();
            return View(movies);
        }

        public async Task<IActionResult> Create()
        {
            var viewModel = new MovieFormViewModel
            {
                Genres = await _context.Genres.OrderBy(x=>x.Name).ToListAsync()
            };
            return View("MovieForm", viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MovieFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.Genres = await _context.Genres.OrderBy(x => x.Name).ToListAsync();
                return View("MovieForm", model);
            }
            var file = Request.Form.Files;
            if(!file.Any())
            {
                model.Genres = await _context.Genres.OrderBy(x => x.Name).ToListAsync();
                ModelState.AddModelError("Poster", "Please Select Movie Poster!");
                return View("MovieForm", model);
            }

            var poster = file.FirstOrDefault();
            

            if(!allowExtentions.Contains(Path.GetExtension(poster.FileName).ToLower()))
            {
                model.Genres = await _context.Genres.OrderBy(x => x.Name).ToListAsync();
                ModelState.AddModelError("Poster", "Only .JPG, .PNG images are allowded!");
                return View("MovieForm", model);
            }

            if (poster.Length > maxAllowedPosterSize)
            {
                model.Genres = await _context.Genres.OrderBy(x => x.Name).ToListAsync();
                ModelState.AddModelError("Poster", "Poster cannot be more than 1 MB");
                return View("MovieForm", model);
            }

            using var dataStream = new MemoryStream();
            await poster.CopyToAsync(dataStream);

            var movie = new Movie
            {
                Title = model.Title,
                GenreId = model.GenreId,
                Year = model.Year,
                Rate = model.Rate,
                StoryLine = model.StoryLine,
                Poster = dataStream.ToArray()
            };
            _context.Movies.Add(movie);
            _context.SaveChanges();

            _toastNotification.AddSuccessToastMessage("Movie Created Successfully");
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return BadRequest();

            var movie = await _context.Movies.FindAsync(id);
            if (movie == null)
                return NotFound();
            var viewModel = new MovieFormViewModel
            {
                Id= movie.Id, 
                Title = movie.Title, 
                GenreId = movie.GenreId, 
                Rate = movie.Rate, 
                Year = movie.Year, 
                StoryLine = movie.StoryLine, 
                Poster = movie.Poster, 
                Genres = await _context.Genres.OrderBy(x => x.Name).ToListAsync()
        };

            return View("MovieForm", viewModel);

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(MovieFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.Genres = await _context.Genres.OrderBy(x => x.Name).ToListAsync();
                return View("MovieForm", model);
            }

            var movie = await _context.Movies.FindAsync(model.Id);
            if (movie == null)
                return NotFound();

            var files = Request.Form.Files;
            if(files.Any())
            {
                var poster = files.FirstOrDefault();
                using var dataStream = new MemoryStream();
                await poster.CopyToAsync(dataStream);

                model.Poster = dataStream.ToArray();

                if (!allowExtentions.Contains(Path.GetExtension(poster.FileName).ToLower()))
                {
                    model.Genres = await _context.Genres.OrderBy(x => x.Name).ToListAsync();
                    ModelState.AddModelError("Poster", "Only .JPG, .PNG images are allowded!");
                    return View("MovieForm", model);
                }

                if (poster.Length > maxAllowedPosterSize)
                {
                    model.Genres = await _context.Genres.OrderBy(x => x.Name).ToListAsync();
                    ModelState.AddModelError("Poster", "Poster cannot be more than 1 MB");
                    return View("MovieForm", model);
                }
                movie.Poster = model.Poster;
            }

            movie.Title = model.Title;
            movie.GenreId = model.GenreId;
            movie.Year = model.Year;
            movie.Rate = model.Rate;
            movie.StoryLine = model.StoryLine;

            _context.SaveChanges();

            _toastNotification.AddSuccessToastMessage("Movie Updated Successfully");
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return BadRequest();

            var movie = await _context.Movies.Include(x => x.Genre).SingleOrDefaultAsync(m => m.Id == id);

            if (movie == null)
                return NotFound();

            return View(movie);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return BadRequest();

            var movie = await _context.Movies.FindAsync(id);

            if (movie == null)
                return NotFound();

            _context.Movies.Remove(movie);
            _context.SaveChanges();

            return Ok();
        }
    }
}
