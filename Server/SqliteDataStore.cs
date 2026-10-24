using BusTicket.Core;
using Microsoft.EntityFrameworkCore;

namespace BusTicket.Api;

/// <summary>Base SQLite (fichier busticket.db). Les tables sont créées au premier démarrage.</summary>
public sealed class BusTicketDb(DbContextOptions<BusTicketDb> options) : DbContext(options)
{
    public DbSet<UserRecord> Users => Set<UserRecord>();
    public DbSet<BookingRecord> Bookings => Set<BookingRecord>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<UserRecord>(user =>
        {
            user.HasKey(u => u.Id);
            user.HasIndex(u => u.Email).IsUnique();
            user.HasIndex(u => u.PhoneDigits);
            user.Property(u => u.Email).UseCollation("NOCASE");
        });

        model.Entity<BookingRecord>(booking =>
        {
            booking.HasKey(b => b.Reference);
            booking.HasIndex(b => b.UserId);
            booking.HasIndex(b => b.TripId);
            booking.Property(b => b.PaymentMethod).HasConversion<string>();
            booking.Property(b => b.Status).HasConversion<string>();
            booking.Ignore(b => b.SeatNumbers);
        });
    }
}

/// <summary><see cref="IDataStore"/> au-dessus d'EF Core. Une instance par requête HTTP.</summary>
public sealed class SqliteDataStore(BusTicketDb db) : IDataStore
{
    public UserRecord? GetUser(string id) => db.Users.Find(id);

    public UserRecord? FindUserByEmail(string email) => db.Users.FirstOrDefault(u => u.Email == email);

    public UserRecord? FindUserByPhone(string phoneDigits) =>
        phoneDigits.Length == 0 ? null : db.Users.FirstOrDefault(u => u.PhoneDigits == phoneDigits);

    public void AddUser(UserRecord user)
    {
        db.Users.Add(user);
        db.SaveChanges();
    }

    public void UpdateUser(UserRecord user) => db.SaveChanges();

    public BookingRecord? FindBooking(string reference) =>
        db.Bookings.FirstOrDefault(b => b.Reference == reference.ToUpper());

    public IReadOnlyList<BookingRecord> BookingsOfUser(string userId) =>
        db.Bookings.Where(b => b.UserId == userId).ToList();

    public IReadOnlyList<int> SoldSeats(string tripId) =>
        db.Bookings.Where(b => b.TripId == tripId).Select(b => b.Seats).AsEnumerable()
          .SelectMany(s => s.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse))
          .ToList();

    public void AddBooking(BookingRecord booking)
    {
        db.Bookings.Add(booking);
        db.SaveChanges();
    }

    public void UpdateBooking(BookingRecord booking) => db.SaveChanges();
}
