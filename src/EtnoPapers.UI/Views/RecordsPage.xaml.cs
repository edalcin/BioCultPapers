using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using EtnoPapers.Core.Models;
using EtnoPapers.Core.Utils;
using EtnoPapers.UI.ViewModels;
using Microsoft.Win32;

namespace EtnoPapers.UI.Views
{
    /// <summary>
    /// Interaction logic for RecordsPage.xaml
    /// Displays, filters, and manages local records with CRUD operations.
    /// </summary>
    public partial class RecordsPage : Page
    {
        private RecordsViewModel? _viewModel;

        public RecordsPage()
        {
            InitializeComponent();
            _viewModel = new RecordsViewModel();
            DataContext = _viewModel;

            // Load records on initial page load
            _viewModel.LoadRecords();

            // Reload records every time the page is navigated to
            Loaded += (s, e) => _viewModel?.LoadRecords();
        }

        /// <summary>
        /// Handle click event for "New Record" button - opens NewRecordDialog
        /// </summary>
        private void NewRecordButton_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel == null)
                return;

            var dialog = new NewRecordDialog();
            if (dialog.ShowDialog() == true && dialog.CreatedRecord != null)
            {
                if (_viewModel.SaveNewRecord(dialog.CreatedRecord))
                {
                    MessageBox.Show(
                        $"Novo registro criado com sucesso!\\n\\nID: {dialog.CreatedRecord.Id}",
                        "Sucesso",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show(
                        "Erro ao criar novo registro.",
                        "Erro",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
        }

        /// <summary>
        /// Handle click event for "Edit" button - opens EditRecordDialog
        /// </summary>
        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "Por favor, selecione um registro para editar.",
                "Nenhum Registro Selecionado",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        /// <summary>
        /// Handle click event for "Delete" button - shows confirmation dialog
        /// </summary>
        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "Por favor, selecione um ou mais registros para deletar.",
                "Nenhum Registro Selecionado",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        /// <summary>
        /// Keeps the ViewModel's SelectedRecords in sync with the DataGrid selection,
        /// so Export (and future bulk actions) can operate on the current selection.
        /// </summary>
        private void RecordsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_viewModel == null)
                return;

            _viewModel.SelectedRecords.Clear();
            foreach (var item in RecordsDataGrid.SelectedItems.Cast<ArticleRecord>())
            {
                _viewModel.SelectedRecords.Add(item);
            }
        }

        /// <summary>
        /// Exports the selected records (or all filtered records, if none selected)
        /// as a JSON array of ArticleRecord, ready to be imported into BioCultDB via
        /// `node backend/src/scripts/import-papers.js &lt;arquivo.json&gt;` (DA6, ADR-005).
        /// </summary>
        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel == null)
                return;

            var recordsToExport = (_viewModel.SelectedRecords.Count > 0
                ? _viewModel.SelectedRecords
                : _viewModel.FilteredRecords).ToList();

            if (recordsToExport.Count == 0)
            {
                MessageBox.Show(
                    "Não há registros para exportar.",
                    "Exportar para BioCultDB",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            var dialog = new SaveFileDialog
            {
                Filter = "JSON files (*.json)|*.json",
                FileName = $"biocultpapers-export-{DateTime.Now:yyyyMMdd}.json",
            };

            if (dialog.ShowDialog() != true)
                return;

            try
            {
                var json = JsonSerializationHelper.SerializeList(recordsToExport);
                File.WriteAllText(dialog.FileName, json);

                MessageBox.Show(
                    $"{recordsToExport.Count} registro(s) exportado(s) com sucesso para:\n{dialog.FileName}\n\n" +
                    "Importe no BioCultDB com:\nnode backend/src/scripts/import-papers.js <arquivo.json>",
                    "Exportação concluída",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Erro ao exportar registros: {ex.Message}",
                    "Erro",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}
