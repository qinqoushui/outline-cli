using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using OutlineVsix.Models;
using OutlineVsix.Services;

namespace OutlineVsix.Views;

public partial class DocumentPickerWindow : Window
{
    private readonly OutlineApiService _api;

    public Document? SelectedDocument { get; private set; }

    public DocumentPickerWindow(OutlineApiService api)
    {
        InitializeComponent();
        _api = api;
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadTreeAsync();
    }

    private async void Refresh_Click(object sender, RoutedEventArgs e)
    {
        await LoadTreeAsync();
    }

    private async Task LoadTreeAsync()
    {
        DocTree.Items.Clear();
        LoadingText.Visibility = Visibility.Visible;

        try
        {
            var collections = await _api.GetCollectionsAsync();
            var allDocs = await _api.GetDocumentsAsync();

            foreach (var col in collections)
            {
                var colNode = new TreeViewItem
                {
                    Header = $"📁 {col.Name}",
                    Tag = new OutlineTreeNode { Id = col.Id, Name = col.Name, Type = NodeType.Collection },
                    IsExpanded = true
                };

                var colDocs = allDocs.Where(d => d.CollectionId == col.Id).ToList();
                AddDocumentsToNode(colNode, colDocs, allDocs);
                DocTree.Items.Add(colNode);
            }

            var orphanDocs = allDocs.Where(d => !collections.Any(c => c.Id == d.CollectionId)).ToList();
            if (orphanDocs.Count > 0)
            {
                var orphanNode = new TreeViewItem
                {
                    Header = "📁 未分类",
                    IsExpanded = true
                };
                AddDocumentsToNode(orphanNode, orphanDocs, allDocs);
                DocTree.Items.Add(orphanNode);
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"加载失败: {ex.Message}", "Outline Wiki",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            LoadingText.Visibility = Visibility.Collapsed;
        }
    }

    private void AddDocumentsToNode(TreeViewItem parent, List<Document> docs, List<Document> allDocs)
    {
        var topLevel = docs.Where(d => string.IsNullOrEmpty(d.ParentDocumentId)).ToList();
        foreach (var doc in topLevel)
        {
            var docNode = new TreeViewItem
            {
                Header = $"📄 {doc.Title}",
                Tag = doc
            };

            var children = allDocs.Where(d => d.ParentDocumentId == doc.Id).ToList();
            if (children.Count > 0)
            {
                AddDocumentsToNode(docNode, children, allDocs);
            }

            parent.Items.Add(docNode);
        }
    }

    private void DocTree_MouseDoubleClick(object sender, RoutedEventArgs e)
    {
        if (DocTree.SelectedItem is TreeViewItem item && item.Tag is Document doc)
        {
            SelectedDocument = doc;
            DialogResult = true;
        }
    }
}
