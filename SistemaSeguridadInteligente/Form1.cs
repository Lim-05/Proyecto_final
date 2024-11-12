using System;
using System.IO.Ports;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace SistemaSeguridadInteligente
{
    public partial class Form1 : Form
    {
        SerialPort serialPort;
        Database db;
        private string ultimoEstadoMovimiento = ""; // Variable para almacenar el último estado del sensor PIR

        public Form1()
        {
            InitializeComponent();
            serialPort = new SerialPort("COM7", 9600);
            serialPort.DataReceived += new SerialDataReceivedEventHandler(DatosRecibidos);

            db = new Database();

            dgvEventos.Columns.Add("Sensor", "Sensor");
            dgvEventos.Columns.Add("TipoEvento", "Tipo de Evento");
            dgvEventos.Columns.Add("FechaHora", "Fecha y Hora");
        }

        private void btnIniciar_Click(object sender, EventArgs e)
        {
            try
            {
                serialPort.Open();
                lblEstadoSistema.Text = "Sistema en modo Vigilante";
                lblEstadoSistema.BackColor = System.Drawing.Color.Green;
                MessageBox.Show("Sistema iniciado y escuchando los sensores.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al iniciar el puerto serial: " + ex.Message);
            }
        }

        // Detener el sistema
        private void btnDetener_Click(object sender, EventArgs e)
        {
            try
            {
                serialPort.Close();
                lblEstadoSistema.Text = "Sistema detenido";
                lblEstadoSistema.BackColor = System.Drawing.Color.Red;
                MessageBox.Show("Sistema detenido.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al detener el puerto serial: " + ex.Message);
            }
        }

        // Datos recibidos desde el puerto serial
        private void DatosRecibidos(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                string data = serialPort.ReadLine().Trim(); // Limpia espacios en blanco
                Console.WriteLine("Datos recibidos: " + data); // Depuración de datos recibidos
                ProcesarDatosSensor(data);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al recibir datos del sensor: " + ex.Message);
            }
        }

        // Procesar los datos recibidos del sensor
        private void ProcesarDatosSensor(string data)
        {
            string sensor = "";
            string tipoEvento = "";
            int cantidadActivaciones = 1;
            DateTime fechaHora = DateTime.Now;

            if (data.Contains("¡Intrusos!"))
            {
                sensor = "Sensor PIR";
                tipoEvento = "Movimiento Detectado";
            }
            else if (data.Contains("Modo vigilante"))
            {
                sensor = "Sensor PIR";
                tipoEvento = "Sin Movimiento";
            }
            else if (data.Contains("Success"))
            {
                sensor = "Teclado";
                tipoEvento = "Contraseña Correcta";
            }
            else if (data.Contains("Wrong"))
            {
                sensor = "Teclado";
                tipoEvento = "Contraseña Incorrecta";
            }

            // Verificar el último estado del sensor PIR
            if (sensor == "Sensor PIR")
            {
                if (tipoEvento == ultimoEstadoMovimiento)
                {
                    // Si el estado es el mismo que el último registrado, no hace nada
                    return;
                }
                else
                {
                    // Actualiza el último estado registrado solo si cambia
                    ultimoEstadoMovimiento = tipoEvento;
                }
            }

            if (!string.IsNullOrEmpty(tipoEvento))
            {
                // Inserta en la base de datos
                db.InsertarEvento(sensor, tipoEvento, fechaHora, cantidadActivaciones);

                // Actualiza el DataGridView
                Invoke(new Action(() =>
                {
                    dgvEventos.Rows.Add(sensor, tipoEvento, fechaHora.ToString());
                    lblEstadoSistema.Text = "Evento registrado: " + tipoEvento;
                }));
            }
        }

        private void dgvEventos_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }
    }

    // Clase para manejar la base de datos
    public class Database
    {
        private MySqlConnection conexion;

        public Database()
        {
            string connectionString = "server=localhost;database=sistema_seguridad_inteligente;uid=root;pwd=Greci@Esponda;";
            conexion = new MySqlConnection(connectionString);
        }

        // Insertar eventos en la base de datos
        public void InsertarEvento(string sensor, string tipoEvento, DateTime fechaHora, int cantidadActivaciones)
        {
            try
            {
                conexion.Open();
                string query = "INSERT INTO eventos (sensor, tipo_evento, fecha, hora, cantidad_activaciones) " +
                               "VALUES (@sensor, @tipoEvento, @fecha, @hora, @cantidadActivaciones)";

                MySqlCommand cmd = new MySqlCommand(query, conexion);
                cmd.Parameters.AddWithValue("@sensor", sensor);
                cmd.Parameters.AddWithValue("@tipoEvento", tipoEvento);
                cmd.Parameters.AddWithValue("@fecha", fechaHora.Date);
                cmd.Parameters.AddWithValue("@hora", fechaHora.TimeOfDay);
                cmd.Parameters.AddWithValue("@cantidadActivaciones", cantidadActivaciones);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                // Manejar error si es necesario
            }
            finally
            {
                conexion.Close();
            }
        }
    }
}