using Microsoft.Data.SqlClient;
using System;
using Microsoft.EntityFrameworkCore;

namespace LibraryConsoleApp
{
    class Program
    {
        private const string ConnectionString = "Server=localhost;Database=YourDatabaseName;Trusted_Connection=True;Encrypt=True;";


        static void Main(string[] args)
        {
            Console.WriteLine("Witaj w aplikacji bibliotecznej!");

            while (true)
            {
                Console.WriteLine("\nWybierz opcję:");
                Console.WriteLine("1. Wyświetl dostępne książki");
                Console.WriteLine("2. Wypożycz książkę");
                Console.WriteLine("3. Wyjście");

                Console.Write("Twój wybór: ");
                string choice = Console.ReadLine();

                switch (choice)
                {
                    case "1":
                        DisplayAvailableBooks();
                        break;
                    case "2":
                        RentBook();
                        break;
                    case "3":
                        Environment.Exit(0);
                        break;
                    default:
                        Console.WriteLine("Niepoprawny wybór. Spróbuj ponownie.");
                        break;
                }
            }
        }

        static void DisplayAvailableBooks()
        {
            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                connection.Open();

                using (SqlCommand command = new SqlCommand("SELECT * FROM Books WHERE Available = 1", connection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        Console.WriteLine("\nDostępne książki:");
                        while (reader.Read())
                        {
                            Console.WriteLine($"{reader["BookId"]}. {reader["Title"]} - {reader["Author"]} ({reader["PublishedYear"]})");
                        }
                    }
                }
            }
        }

        static void RentBook()
        {
            Console.Write("\nPodaj numer książki do wypożyczenia: ");
            if (int.TryParse(Console.ReadLine(), out int bookId))
            {
                using (SqlConnection connection = new SqlConnection(ConnectionString))
                {
                    connection.Open();

                    using (SqlTransaction transaction = connection.BeginTransaction())
                    {
                        try
                        {
                          
                            using (SqlCommand checkAvailabilityCommand = new SqlCommand(
                                "SELECT Available FROM Books WHERE BookId = @BookId", connection, transaction))
                            {
                                checkAvailabilityCommand.Parameters.AddWithValue("@BookId", bookId);
                                bool available = (bool)checkAvailabilityCommand.ExecuteScalar();

                                if (!available)
                                {
                                    Console.WriteLine("Książka jest już wypożyczona.");
                                    return;
                                }
                            }

                            using (SqlCommand updateCommand = new SqlCommand(
                                "UPDATE Books SET Available = 0 WHERE BookId = @BookId", connection, transaction))
                            {
                                updateCommand.Parameters.AddWithValue("@BookId", bookId);
                                updateCommand.ExecuteNonQuery();
                            }

                            using (SqlCommand insertCommand = new SqlCommand(
                                "INSERT INTO Rentals (BookId, ReaderId, RentalDate) VALUES (@BookId, @ReaderId, @RentalDate)",
                                connection, transaction))
                            {
                                insertCommand.Parameters.AddWithValue("@BookId", bookId);
                                insertCommand.Parameters.AddWithValue("@ReaderId", 1); 
                                insertCommand.Parameters.AddWithValue("@RentalDate", DateTime.Now);
                                insertCommand.ExecuteNonQuery();
                            }

                            transaction.Commit();
                            Console.WriteLine("Książka została wypożyczona pomyślnie.");
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            Console.WriteLine($"Błąd podczas wypożyczania książki: {ex.Message}");
                        }
                    }
                }
            }
            else
            {
                Console.WriteLine("Niepoprawny numer książki. Spróbuj ponownie.");
            }
        }
    }
}
