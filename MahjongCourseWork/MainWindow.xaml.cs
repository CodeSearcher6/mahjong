using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using MahjongCourseWork.Models;
using System.Collections.Generic;
using System.Linq;
using MahjongCourseWork.Services;
using System.IO;
using System.Windows.Media.Imaging;
using MahjongCourseWork.Core;

namespace MahjongCourseWork
{
    public partial class MainWindow : Window
    {
        private readonly DispatcherTimer _timer;
        private readonly Random _random = new Random();
        private int _seconds = 0;
        private int _moves = 0;
        private List<TileInstance> _tiles = new List<TileInstance>();
        private TileInstance? _selectedTile;
        private int _lastRemovedFirstId = -1;
        private int _lastRemovedSecondId = -1;
        private int _lastUndoneFirstId = -1;
        private int _lastUndoneSecondId = -1;
        private bool _canUndo = false;
        private bool _canRedo = false;
        private string _layoutName = LayoutFactory.Classic;
        private readonly MoveValidator _moveValidator = new MoveValidator();
        private readonly HintService _hintService = new HintService();
        private bool _noMovesWarningShown = false;

        public MainWindow()
        {
            InitializeComponent();

            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += Timer_Tick;

            CreateBoard();
            _timer.Start();
            StatusTextBlock.Text = $"Гру розпочато ({_layoutName})";
        }
        private string GetTileImagePath(TileKind kind)
        {
            return kind switch
            {
                TileKind.Bamboo1 => "Assets/Tiles/Sou1.png",
                TileKind.Bamboo2 => "Assets/Tiles/Sou2.png",
                TileKind.Bamboo3 => "Assets/Tiles/Sou3.png",

                TileKind.Character1 => "Assets/Tiles/Man1.png",
                TileKind.Character2 => "Assets/Tiles/Man2.png",
                TileKind.Character3 => "Assets/Tiles/Man3.png",

                TileKind.Circle1 => "Assets/Tiles/Pin1.png",
                TileKind.Circle2 => "Assets/Tiles/Pin2.png",
                TileKind.Circle3 => "Assets/Tiles/Pin3.png",

                TileKind.DragonRed => "Assets/Tiles/Chun.png",
                TileKind.DragonGreen => "Assets/Tiles/Hatsu.png",
                TileKind.WindEast => "Assets/Tiles/Ton.png",

                _ => string.Empty
            };
        }
        private void Timer_Tick(object? sender, EventArgs e)
        {
            _seconds++;
            TimerTextBlock.Text = TimeSpan.FromSeconds(_seconds).ToString(@"mm\:ss");
        }

        private void CreateBoard()
        { 
            GameBoard.Children.Clear();
            _tiles.Clear();
            _selectedTile = null;
            _lastRemovedFirstId = -1;
            _lastRemovedSecondId = -1;
            _lastUndoneFirstId = -1;
            _lastUndoneSecondId = -1;
            _canUndo = false;
            _canRedo = false;

            var kinds = new List<TileKind>
            {
                TileKind.Bamboo1, TileKind.Bamboo1,
                TileKind.Bamboo2, TileKind.Bamboo2,
                TileKind.Bamboo3, TileKind.Bamboo3,
                TileKind.Character1, TileKind.Character1,
                TileKind.Character2, TileKind.Character2,
                TileKind.Character3, TileKind.Character3,
                TileKind.Circle1, TileKind.Circle1,
                TileKind.Circle2, TileKind.Circle2,
                TileKind.Circle3, TileKind.Circle3,
                TileKind.DragonRed, TileKind.DragonRed,
                TileKind.DragonGreen, TileKind.DragonGreen,
                TileKind.WindEast, TileKind.WindEast
            };

            var positions = LayoutFactory.GetLayoutPositions(_layoutName);

            if (positions.Count < kinds.Count)
            {
                throw new InvalidOperationException("Розкладка містить замало позицій для поточного набору плиток.");
            }

            for (int i = 0; i < kinds.Count; i++)
            {
                var tile = new TileInstance
                {
                    Id = i + 1,
                    Kind = kinds[i],
                    Row = positions[i].Row,
                    Column = positions[i].Column,
                    Layer = 0,
                    IsRemoved = false,
                    IsSelected = false
                };

                _tiles.Add(tile);
            }

            for (int row = 0; row < 6; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    var tile = _tiles.FirstOrDefault(t => t.Row == row && t.Column == col);

                    if (tile == null)
                    {
                        var emptyBorder = new Border
                        {
                            Background = System.Windows.Media.Brushes.Transparent,
                            Margin = new Thickness(6)
                        };

                        GameBoard.Children.Add(emptyBorder);
                        continue;
                    }

                    var button = new Button
                    {
                        Content = CreateTileContent(tile),
                        Height = 90,
                        Margin = new Thickness(4),
                        Tag = tile,
                        Background = System.Windows.Media.Brushes.White,
                        BorderBrush = System.Windows.Media.Brushes.Gray,
                        BorderThickness = new Thickness(1),
                        Padding = new Thickness(2)
                    };

                    button.Click += TileButton_Click;
                    GameBoard.Children.Add(button);
                }
            }

            RefreshTiles();
            EnsurePlayableStart();
            UpdatePairsCounter();
            StatusTextBlock.Text = $"Поле створено ({_layoutName})";
            CheckForNoMoves();
        }

        private object CreateTileContent(TileInstance tile)
        {
            string relativePath = GetTileImagePath(tile.Kind);

            if (!string.IsNullOrWhiteSpace(relativePath))
            {
                string fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativePath);

                if (File.Exists(fullPath))
                {
                    return new Image
                    {
                        Source = new BitmapImage(new Uri(fullPath, UriKind.Absolute)),
                        Stretch = System.Windows.Media.Stretch.Uniform,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    };
                }
            }

            return new TextBlock
            {
                Text = tile.DisplayName,
                FontSize = 13,
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
        }
        private void RefreshTiles()
        {
            foreach (var child in GameBoard.Children)
            {
                if (child is Button btn && btn.Tag is TileInstance tile)
                {
                    if (tile.IsRemoved)
                    {
                        btn.Visibility = Visibility.Hidden;
                        continue;
                    }

                    bool isFree = _moveValidator.IsTileFree(tile, _tiles);

                    btn.IsEnabled = true;
                    btn.Opacity = isFree ? 1.0 : 0.55;

                    if (!tile.IsSelected)
                    {
                        btn.BorderThickness = new Thickness(1);
                        btn.BorderBrush = System.Windows.Media.Brushes.Gray;
                        btn.Background = System.Windows.Media.Brushes.White;
                    }
                }
            }
        }
        private void TileButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button || button.Tag is not TileInstance clickedTile)
                return;

            if (clickedTile.IsRemoved)
                return;

            if (!_moveValidator.IsTileFree(clickedTile, _tiles))
            {
                StatusTextBlock.Text = $"Плитка {clickedTile.DisplayName} заблокована";
                return;
            }

            if (_selectedTile == null)
            {
                ClearSelectionVisuals();

                _selectedTile = clickedTile;
                clickedTile.IsSelected = true;
                button.BorderThickness = new Thickness(3);
                button.BorderBrush = System.Windows.Media.Brushes.DodgerBlue;
                button.Background = System.Windows.Media.Brushes.AliceBlue;

                StatusTextBlock.Text = $"Вибрано плитку: {clickedTile.DisplayName}";
                return;
            }

            if (_selectedTile.Id == clickedTile.Id)
            {
                ClearSelectionVisuals();
                _selectedTile = null;
                StatusTextBlock.Text = "Вибір скасовано";
                return;
            }

            if (_moveValidator.CanMatch(_selectedTile, clickedTile, _tiles))
            {
                _lastRemovedFirstId = _selectedTile.Id;
                _lastRemovedSecondId = clickedTile.Id;
                _canUndo = true;

                _lastUndoneFirstId = -1;
                _lastUndoneSecondId = -1;
                _canRedo = false;

                RemoveTileFromBoard(_selectedTile);
                RemoveTileFromBoard(clickedTile);

                _moves++;
                MovesTextBlock.Text = _moves.ToString();

                _selectedTile = null;
                ClearSelectionVisuals();
                RefreshTiles();
                UpdatePairsCounter();

                StatusTextBlock.Text = $"Пару {clickedTile.DisplayName} видалено";

                CheckForWin();

                if (!_tiles.All(t => t.IsRemoved))
                {
                    CheckForNoMoves();
                }
            }
            else
            {
                ClearSelectionVisuals();

                _selectedTile = clickedTile;
                clickedTile.IsSelected = true;
                button.BorderThickness = new Thickness(3);
                button.BorderBrush = System.Windows.Media.Brushes.DodgerBlue;
                button.Background = System.Windows.Media.Brushes.AliceBlue;

                StatusTextBlock.Text = $"Обрано нову плитку: {clickedTile.DisplayName}";
            }
        }

        

        private void Hint_Click(object sender, RoutedEventArgs e)
        {
            ClearSelectionVisuals();

            var hint = _hintService.FindAvailablePair(_tiles);

            if (hint.First == null || hint.Second == null)
            {
                StatusTextBlock.Text = "Доступних пар не знайдено";
                return;
            }

            HighlightHintTile(hint.First);
            HighlightHintTile(hint.Second);

            StatusTextBlock.Text = $"Підказка: {hint.First.DisplayName} + {hint.Second.DisplayName}";
        }
        private void HighlightHintTile(TileInstance tile)
        {
            foreach (var child in GameBoard.Children)
            {
                if (child is Button btn && btn.Tag is TileInstance currentTile && currentTile.Id == tile.Id)
                {
                    btn.BorderThickness = new Thickness(4);
                    btn.BorderBrush = System.Windows.Media.Brushes.ForestGreen;
                    btn.Background = System.Windows.Media.Brushes.LightGoldenrodYellow;
                    break;
                }
            }
        }

        private void Shuffle_Click(object sender, RoutedEventArgs e)
        {
            if (_tiles.All(t => t.IsRemoved))
            {
                StatusTextBlock.Text = "Усі плитки вже видалено";
                return;
            }

            ShuffleRemainingTiles();
            UpdatePairsCounter();
            StatusTextBlock.Text = "Невидалені плитки перемішано";

            CheckForNoMoves();
        }

        private void Undo_Click(object sender, RoutedEventArgs e)
        {
            if (!_canUndo)
            {
                StatusTextBlock.Text = "Немає ходу для відміни";
                return;
            }

            var first = _tiles.FirstOrDefault(t => t.Id == _lastRemovedFirstId);
            var second = _tiles.FirstOrDefault(t => t.Id == _lastRemovedSecondId);

            if (first == null || second == null)
            {
                StatusTextBlock.Text = "Не вдалося відновити останню пару";
                return;
            }

            RestoreTileOnBoard(first);
            RestoreTileOnBoard(second);

            _moves = Math.Max(0, _moves - 1);
            MovesTextBlock.Text = _moves.ToString();

            _lastUndoneFirstId = _lastRemovedFirstId;
            _lastUndoneSecondId = _lastRemovedSecondId;
            _canRedo = true;

            _lastRemovedFirstId = -1;
            _lastRemovedSecondId = -1;
            _canUndo = false;

            _selectedTile = null;

            ClearSelectionVisuals();
            RefreshTiles();
            UpdatePairsCounter();

            if (!_timer.IsEnabled)
            {
                _timer.Start();
            }

            StatusTextBlock.Text = $"Хід скасовано: {first.DisplayName} + {second.DisplayName}";
        }

        private void Redo_Click(object sender, RoutedEventArgs e)
        {
            if (!_canRedo)
            {
                StatusTextBlock.Text = "Немає ходу для повтору";
                return;
            }

            var first = _tiles.FirstOrDefault(t => t.Id == _lastUndoneFirstId);
            var second = _tiles.FirstOrDefault(t => t.Id == _lastUndoneSecondId);

            if (first == null || second == null || first.IsRemoved || second.IsRemoved)
            {
                _canRedo = false;
                StatusTextBlock.Text = "Не вдалося повторити хід";
                return;
            }

            if (!_moveValidator.CanMatch(first, second, _tiles))
            {
                _canRedo = false;
                StatusTextBlock.Text = "Повтор неможливий у поточному стані поля";
                return;
            }

            RemoveTileFromBoard(first);
            RemoveTileFromBoard(second);

            _moves++;
            MovesTextBlock.Text = _moves.ToString();

            _lastRemovedFirstId = _lastUndoneFirstId;
            _lastRemovedSecondId = _lastUndoneSecondId;
            _canUndo = true;

            _lastUndoneFirstId = -1;
            _lastUndoneSecondId = -1;
            _canRedo = false;

            _selectedTile = null;

            ClearSelectionVisuals();
            RefreshTiles();
            UpdatePairsCounter();

            StatusTextBlock.Text = $"Хід повторено: {first.DisplayName} + {second.DisplayName}";

            CheckForWin();

            if (!_tiles.All(t => t.IsRemoved))
            {
                CheckForNoMoves();
            }
        }

        private void Rules_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "Знаходьте однакові вільні плитки та прибирайте їх з поля.",
                "Правила",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "Mahjong Solitaire\nКурсова робота на тему: Гра Маджонг.",
                "Про програму",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ClassicLayout_Click(object s, RoutedEventArgs e)
        {
            _layoutName = LayoutFactory.Classic;
            ResetAndStart($"Обрано розкладку: {_layoutName}");
        }
        private void FortressLayout_Click(object sender, RoutedEventArgs e)
        {
            _layoutName = LayoutFactory.Fortress;
            ResetAndStart($"Обрано розкладку: {_layoutName}");
        }
        private void NewGame_Click(object s, RoutedEventArgs e) => ResetAndStart($"Розпочато нову гру ({_layoutName})");
        private void Restart_Click(object s, RoutedEventArgs e) => ResetAndStart($"Гру перезапущено ({_layoutName})");

        private int CountPairs()
        {
            int count = 0;

            for (int i = 0; i < _tiles.Count; i++)
            {
                var first = _tiles[i];

                if (first.IsRemoved || !_moveValidator.IsTileFree(first, _tiles))
                    continue;

                for (int j = i + 1; j < _tiles.Count; j++)
                {
                    if (_moveValidator.CanMatch(first, _tiles[j], _tiles))
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        private void UpdatePairsCounter()
        {
            AvailablePairsTextBlock.Text = CountPairs().ToString();
        }

        private void CheckForNoMoves()
        {
            if (_tiles.All(t => t.IsRemoved))
                return;

            if (!HasAvailableMoves())
            {
                if (!_noMovesWarningShown)
                {
                    MessageBox.Show("Доступних ходів більше немає.\nСкористайтеся кнопкою 'Перемішати'.",
                                    "Ходів немає", MessageBoxButton.OK, MessageBoxImage.Warning);
                    _noMovesWarningShown = true;
                }

                StatusTextBlock.Text = "Ходів немає — рекомендується перемішування";
            }
            else
            {
                _noMovesWarningShown = false;
            }
        }


        private bool HasAvailableMoves()
        {
            var hint = _hintService.FindAvailablePair(_tiles);
            return hint.First != null && hint.Second != null;
        }

        private void EnsurePlayableStart()
        {
            int attempts = 0;
            const int maxAttempts = 50;

            while (!HasAvailableMoves() && attempts < maxAttempts)
            {
                ShuffleRemainingTiles(false); // без UI в циклі
                attempts++;
            }
            RefreshBoardTexts();      // один раз після циклу
            ClearSelectionVisuals();
            RefreshTiles();
        }
        private void CheckForWin()
        {
            bool allRemoved = _tiles.All(t => t.IsRemoved);

            if (allRemoved)
            {
                _timer.Stop();

                MessageBox.Show(
                    $"Вітаємо! Ви прибрали всі плитки за {TimerTextBlock.Text}.\nКількість ходів: {_moves}.",
                    "Перемога",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                StatusTextBlock.Text = "Гру завершено перемогою";
            }
        }
        private void ShuffleRemainingTiles(bool refreshUi = true)
        {
            var remainingTiles = _tiles.Where(t => !t.IsRemoved).ToList();
            var shuffledKinds = remainingTiles.Select(t => t.Kind).OrderBy(_ => _random.Next()).ToList();

            for (int i = 0; i < remainingTiles.Count; i++)
                remainingTiles[i].Kind = shuffledKinds[i];

            if (refreshUi)
            {
                RefreshBoardTexts();
                ClearSelectionVisuals();
                RefreshTiles();
            }
        }
        private void RefreshBoardTexts()
        {
            foreach (var child in GameBoard.Children)
            {
                if (child is Button btn && btn.Tag is TileInstance tile)
                {
                    btn.Content = CreateTileContent(tile);
                }
            }
        }
        private void ResetAndStart(string status)
        {
            _seconds = 0;
            _moves = 0;
            TimerTextBlock.Text = "00:00";
            MovesTextBlock.Text = "0";
            _noMovesWarningShown = false;

            CreateBoard();
            _timer.Start();
            StatusTextBlock.Text = status;
        }

        private void RemoveTileFromBoard(TileInstance tile)
        {
            tile.IsRemoved = true;
            tile.IsSelected = false;

            foreach (var child in GameBoard.Children)
            {
                if (child is Button btn && btn.Tag is TileInstance t && t.Id == tile.Id)
                {
                    btn.Visibility = Visibility.Hidden;
                    btn.IsEnabled = false;
                    btn.BorderThickness = new Thickness(1);
                    break;
                }
            }
        }

        private void RestoreTileOnBoard(TileInstance tile)
        {
            tile.IsRemoved = false;
            tile.IsSelected = false;

            foreach (var child in GameBoard.Children)
            {
                if (child is Button btn && btn.Tag is TileInstance t && t.Id == tile.Id)
                {
                    btn.Visibility = Visibility.Visible;
                    btn.IsEnabled = true;
                    btn.Content = CreateTileContent(tile);
                    btn.BorderThickness = new Thickness(1);
                    btn.BorderBrush = System.Windows.Media.Brushes.Gray;
                    btn.Background = System.Windows.Media.Brushes.White;
                    break;
                }
            }
        }

        private void ClearSelectionVisuals()
        {
            foreach (var child in GameBoard.Children)
            {
                if (child is Button btn && btn.Tag is TileInstance tile)
                {
                    tile.IsSelected = false;
                    btn.BorderThickness = new Thickness(1);
                    btn.BorderBrush = System.Windows.Media.Brushes.Gray;
                    btn.Background = System.Windows.Media.Brushes.White;
                }
            }
        }
    }
}
