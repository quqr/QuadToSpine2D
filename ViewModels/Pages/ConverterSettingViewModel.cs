using System;
using CommunityToolkit.Mvvm.ComponentModel;


namespace QTSAvalonia.ViewModels.Pages;

public partial class ConverterSettingViewModel: ViewModelBase
{
    [ObservableProperty]
    private bool isLoopingAnimation;
    [ObservableProperty]
    private bool isPrettyJsonPrint;

    [ObservableProperty] private string scaleFactorStr;

    private float ScaleFactor => int.TryParse(scaleFactorStr, out var res) ? res : 1;
}