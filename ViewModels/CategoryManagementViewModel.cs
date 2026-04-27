using System.Collections.ObjectModel;
using System.Windows.Input;
using SupermarketPOS.Models;
using SupermarketPOS.Services;
using SupermarketPOS.Helpers;

namespace SupermarketPOS.ViewModels;

public class CategoryManagementViewModel : BaseViewModel
{
    private readonly IProductService _productService;
    
    public ObservableCollection<Category> Categories { get; } = new();

    public record NamedColor(string Name, string Hex);
    public ObservableCollection<NamedColor> AvailableColors { get; } = new()
    {
        new("Moviy (Odatiy)", "#2196F3"),
        new("Qizil", "#F44336"),
        new("Pushti", "#E91E63"),
        new("Siyohrang", "#9C27B0"),
        new("To'q ko'k", "#3F51B5"),
        new("Havorang", "#03A9F4"),
        new("Feruza", "#00BCD4"),
        new("Yashil", "#4CAF50"),
        new("Och yashil", "#8BC34A"),
        new("Sariq", "#FFEB3B"),
        new("Qahrabo", "#FFC107"),
        new("To'q sariq", "#FF9800"),
        new("Jigarrang", "#795548"),
        new("Kulrang", "#9E9E9E"),
        new("Qora", "#212121")
    };

    private NamedColor _selectedNamedColor;
    public NamedColor SelectedNamedColor
    {
        get => _selectedNamedColor;
        set
        {
            if (SetProperty(ref _selectedNamedColor, value))
            {
                if (value != null) Color = value.Hex;
            }
        }
    }

    private Category? _selectedCategory;
    public Category? SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            if (SetProperty(ref _selectedCategory, value))
            {
                if (value != null)
                {
                    Name = value.Name;
                    Description = value.Description;
                    Color = value.Color;
                    SelectedNamedColor = AvailableColors.FirstOrDefault(c => c.Hex.Equals(value.Color, StringComparison.OrdinalIgnoreCase)) 
                                         ?? AvailableColors.First();
                    IsEditing = true;
                }
                else
                {
                    ClearForm();
                }
            }
        }
    }

    private string _name = string.Empty;
    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    private string _description = string.Empty;
    public string Description
    {
        get => _description;
        set => SetProperty(ref _description, value);
    }

    private string _color = "#2196F3";
    public string Color
    {
        get => _color;
        set => SetProperty(ref _color, value);
    }

    private bool _isEditing;
    public bool IsEditing
    {
        get => _isEditing;
        set => SetProperty(ref _isEditing, value);
    }

    public ICommand SaveCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand ClearCommand { get; }

    public CategoryManagementViewModel(IProductService productService)
    {
        _productService = productService;

        SaveCommand = new AsyncRelayCommand(SaveAsync, () => !string.IsNullOrWhiteSpace(Name));
        DeleteCommand = new AsyncRelayCommand(DeleteAsync, () => SelectedCategory != null);
        ClearCommand = new RelayCommand(ClearForm);

        _selectedNamedColor = AvailableColors.First();

        _ = LoadCategoriesAsync();
    }

    private async Task LoadCategoriesAsync()
    {
        await RunAsync(async () =>
        {
            var items = await _productService.GetCategoriesAsync();
            Categories.Clear();
            foreach (var item in items)
                Categories.Add(item);
        });
    }

    private async Task SaveAsync()
    {
        await RunAsync(async () =>
        {
            if (IsEditing && SelectedCategory != null)
            {
                SelectedCategory.Name = Name;
                SelectedCategory.Description = Description;
                SelectedCategory.Color = Color;
                await _productService.UpdateCategoryAsync(SelectedCategory);
            }
            else
            {
                var newCat = new Category
                {
                    Name = Name,
                    Description = Description,
                    Color = Color
                };
                await _productService.AddCategoryAsync(newCat);
            }
            await LoadCategoriesAsync();
            ClearForm();
        });
    }

    private async Task DeleteAsync()
    {
        if (SelectedCategory == null) return;
        await RunAsync(async () =>
        {
            await _productService.DeleteCategoryAsync(SelectedCategory.Id);
            await LoadCategoriesAsync();
            ClearForm();
        });
    }

    private void ClearForm()
    {
        SelectedCategory = null;
        Name = string.Empty;
        Description = string.Empty;
        Color = "#2196F3";
        SelectedNamedColor = AvailableColors.First();
        IsEditing = false;
    }
}
