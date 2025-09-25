using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosServer
{
    internal class Program
    {
        static void Main(string[] args)
        {
            using (var context = new ArchsVsDinosConnection())
            {
                try
                {
                    // Este comando forzará la conexión a la base de datos
                    // y mostrará el número de registros en la tabla
                    // Reemplaza 'NombreDeTuTabla' por una tabla real en tu DB
                    var count = context.Player.Count();
                    Console.WriteLine($"Conexión exitosa. Número de registros en la tabla: {count}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error al conectar o consultar: {ex.Message}");
                }
            }

            Console.WriteLine("Presiona cualquier tecla para salir...");
            Console.ReadKey();
        }
    }
}
