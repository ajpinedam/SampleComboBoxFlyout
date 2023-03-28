using Uno.Extensions;

using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.UI.Core;
#if WINUI
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Dispatching;
#else
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using static SampleComboBoxFlyout.MultiFrame;
using System.Linq;
#endif
#if __IOS__
using UIKit;
using _UIViewController = UIKit.UIViewController;
using Uno.UI.Controls;
#else
using _UIViewController = System.Object;
#endif

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace SampleComboBoxFlyout
{
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	public sealed partial class MainPage : Page
	{
		private readonly MultiFrame _multiFrame;

		public MainPage()
		{
			this.InitializeComponent();
			_multiFrame = this.NavigationRoot;
		}

		async void OpenModal(object sender, RoutedEventArgs e)
		{
			var modal = new ModalPage();

			var transitionInfo = OpenModalTransitionInfo;

			await _multiFrame.OpenModal((FrameSectionsTransitionInfo)transitionInfo, modal);
		}

		public SectionsTransitionInfo OpenModalTransitionInfo { get; set; } =
#if __IOS__
			FrameSectionsTransitionInfo.NativeiOSModal;
#else
			FrameSectionsTransitionInfo.SlideUp;
#endif
	}




	public partial class MultiFrame : Grid
	{
		private readonly Dictionary<string, FrameInfo> _frames = new Dictionary<string, FrameInfo>();
		private readonly TaskCompletionSource<bool> _isReady = new TaskCompletionSource<bool>();

#if WINUI
		private DispatcherQueue _dispatcher => DispatcherQueue;
#else
		private CoreDispatcher _dispatcher => Dispatcher;
#endif

		public MultiFrame()
		{
			Loaded += OnLoaded;
        }

		private void OnLoaded(object sender, RoutedEventArgs e)
		{
			_isReady.TrySetResult(true);
		}

		public async Task OpenModal(FrameSectionsTransitionInfo transitionInfo, Page page) // Runs on background thread.
		{
			//await HidePreviousFrameAndShowNextFrame(previousFrameName, nextFrameName, UpdateView);

			//var uiViewController = new UiViewController(page);
#if __IOS__

			//var frame = new Frame();
			//frame.AddChild(page);

            var uiViewController = new UiViewController(page);

            var rootController = UIKit.UIApplication.SharedApplication.KeyWindow.RootViewController;

			await rootController.PresentViewControllerAsync(uiViewController, animated: true);
#else
			await Task.CompletedTask;
#endif
		}

		private enum FrameState
		{
			Hidden,
			Showing,
			Shown,
			Hiding
		}

		/// <summary>
		/// This class is used to associate properties with a <see cref="Frame"/> instance.
		/// </summary>
		private class FrameInfo
		{
			public FrameInfo(Frame frame, int index, int priority, ModalViewController modalViewController = null)
			{
				Frame = frame;
				Index = index;
				Priority = priority;
				ModalViewController = modalViewController;
				State = FrameState.Hidden;
			}

			/// <summary>
			/// The frame associated with the other properties.
			/// </summary>
			public Frame Frame { get; }

			/// <summary>
			/// The state of the frame visually.
			/// This is used to cleanup frames when changing states.
			/// </summary>
			public FrameState State { get; set; }

			/// <summary>
			/// The index of the frame in the its parent <see cref="Panel.Children"/> collection.
			/// This is used to chose which animation to use when changing sections.
			/// </summary>
			public int Index { get; set; }

			/// <summary>
			/// The priority used to order frames in their parent <see cref="Panel.Children"/> collection.
			/// This ensures that modal are on top of sections and that modals are ordered even when opened with custom priorities.
			/// </summary>
			public int Priority { get; }

			public ModalViewController ModalViewController { get; }
		}

		public class UiViewController : _UIViewController
		{
			public UiViewController(Page frame)
			{
#if __IOS__
                View = frame;
#endif
			}

            public UiViewController(Frame frame)
            {
#if __IOS__
                View = frame;
#endif
            }

            public UIViewControllerSectionsTransitionInfo OpeningTransitionInfo { get; set; }

			public void SetTransitionInfo(UIViewControllerSectionsTransitionInfo transitionInfo)
			{
				ModalInPresentation = !transitionInfo.AllowDismissFromGesture;
				ModalPresentationStyle = transitionInfo.ModalPresentationStyle;
				ModalTransitionStyle = transitionInfo.ModalTransitionStyle;
			}
		}

		public class ModalViewController : _UIViewController, IDisposable
		{
#if __IOS__
			private bool _isClosingProgrammatically;
			private bool _wasClosedNatively;
#endif

			/// <summary>
			/// Creates a new instance of <see cref="ModalViewController"/>.
			/// </summary>
			/// <param name="modalName">The modal name.</param>
			/// <param name="frame">The frame to wrap.</param>
			public ModalViewController(string modalName, Frame frame)
			{
				ModalName = modalName;
				frame.Visibility = Visibility.Visible;
				frame.Opacity = 1;
				frame.IsHitTestVisible = true;
#if __IOS__
				View = frame;
#endif
			}

			/// <summary>
			/// The modal name associated with this UIViewController.
			/// </summary>
			public string ModalName { get; }

			/// <summary>
			/// The <see cref="UIViewControllerSectionsTransitionInfo"/> used when opening this modal.
			/// This is reused for the closing transition info when closing natively.
			/// </summary>
			public UIViewControllerSectionsTransitionInfo OpeningTransitionInfo { get; private set; }

			/// <summary>
			/// This event is raised when the UIViewController was closed natively, meaning that the <see cref="ISectionsNavigator"/> was not responsible for the close operation.
			/// This can happen when the user uses a native gesture.
			/// </summary>
#pragma warning disable CS0414 // This event is only used on iOS. However, it's still available on other platforms to simplify the code.
			public event EventHandler ClosedNatively;
#pragma warning restore CS0414

			/// <summary>
			/// Opens this UIViewController.
			/// </summary>
			/// <param name="transitionInfo">The transition info affecting the native animation.</param>
			public async Task Open(UIViewControllerSectionsTransitionInfo transitionInfo)
			{
				OpeningTransitionInfo = transitionInfo;
#if __IOS__
				SetTransitionInfo(transitionInfo);

				var rootController = UIKit.UIApplication.SharedApplication.KeyWindow.RootViewController;

				await rootController.PresentViewControllerAsync(this, animated: true);
#else
				await Task.CompletedTask;
#endif
			}

			/// <summary>
			/// Closes this UIViewController.
			/// </summary>
			/// <param name="transitionInfo">The transition info affecting the native animation.</param>
			public async Task Close(UIViewControllerSectionsTransitionInfo transitionInfo)
			{
#if __IOS__
				if (_wasClosedNatively)
				{
					return;
				}

				_isClosingProgrammatically = true;
				SetTransitionInfo(transitionInfo);

				await DismissViewControllerAsync(animated: true);
#else
				await Task.CompletedTask;
#endif
			}

#if __IOS__
			private void SetTransitionInfo(UIViewControllerSectionsTransitionInfo transitionInfo)
			{
				ModalInPresentation = !transitionInfo.AllowDismissFromGesture;
				ModalPresentationStyle = transitionInfo.ModalPresentationStyle;
				ModalTransitionStyle = transitionInfo.ModalTransitionStyle;
			}

			///<inheritdoc/>
			public override void ViewDidDisappear(bool animated)
			{
				if (!_isClosingProgrammatically)
				{
					_wasClosedNatively = true;

					ClosedNatively?.Invoke(this, EventArgs.Empty);
				}
			}

			///<inheritdoc/>
			protected override void Dispose(bool disposing)
			{
				base.Dispose(disposing);

				ClosedNatively = null;
			}
#else
			///<inheritdoc/>
			public void Dispose()
			{
				ClosedNatively = null;
			}
#endif
		}

		public abstract class FrameSectionsTransitionInfo : SectionsTransitionInfo
		{
			/// <summary>
			/// The type of <see cref="FrameSectionsTransitionInfo"/>.
			/// </summary>
			public abstract FrameSectionsTransitionInfoTypes Type { get; }

			/// <summary>
			/// Gets the transition info for a suppressed transition. There is not visual animation when using this transition info.
			/// </summary>
			public static DelegatingFrameSectionsTransitionInfo SuppressTransition { get; } = new DelegatingFrameSectionsTransitionInfo(ExecuteSuppressTransition);

			/// <summary>
			/// The new frame fades in or the previous frame fades out, depending on the layering.
			/// </summary>
			public static DelegatingFrameSectionsTransitionInfo FadeInOrFadeOut { get; } = new DelegatingFrameSectionsTransitionInfo(ExecuteFadeInOrFadeOut);

			/// <summary>
			/// The new frame slides up, hiding the previous frame.
			/// </summary>
			public static DelegatingFrameSectionsTransitionInfo SlideUp { get; } = new DelegatingFrameSectionsTransitionInfo(ExecuteSlideUp);

			/// <summary>
			/// The previous frame slides down, revealing the new frame.
			/// </summary>
			public static DelegatingFrameSectionsTransitionInfo SlideDown { get; } = new DelegatingFrameSectionsTransitionInfo(ExecuteSlideDown);

			/// <summary>
			/// The frames are animated using a UIViewController with the default configuration.
			/// </summary>
			public static UIViewControllerSectionsTransitionInfo NativeiOSModal { get; } = new UIViewControllerSectionsTransitionInfo();

			private static Task ExecuteSlideDown(Frame frameToHide, Frame frameToShow, bool frameToShowIsAboveFrameToHide)
			{
				return Animations.SlideFrame1DownToRevealFrame2(frameToHide, frameToShow);
			}

			private static Task ExecuteSlideUp(Frame frameToHide, Frame frameToShow, bool frameToShowIsAboveFrameToHide)
			{
				return Animations.SlideFrame2UpwardsToHideFrame1(frameToHide, frameToShow);
			}

			private static Task ExecuteFadeInOrFadeOut(Frame frameToHide, Frame frameToShow, bool frameToShowIsAboveFrameToHide)
			{
				if (frameToShowIsAboveFrameToHide)
				{
					return Animations.FadeInFrame2ToHideFrame1(frameToHide, frameToShow);
				}
				else
				{
					return Animations.FadeOutFrame1ToRevealFrame2(frameToHide, frameToShow);
				}
			}

			private static Task ExecuteSuppressTransition(Frame frameToHide, Frame frameToShow, bool frameToShowIsAboveFrameToHide)
			{
				return Animations.CollapseFrame1AndShowFrame2(frameToHide, frameToShow);
			}
		}

		public enum FrameSectionsTransitionInfoTypes
		{
			/// <summary>
			/// The transition is applied by changing properties or animating properties of <see cref="Frame"/> objects.
			/// This is associated with the <see cref="DelegatingFrameSectionsTransitionInfo"/> class.
			/// </summary>
			FrameBased,

			/// <summary>
			/// The transition is applied by using the native iOS transitions offered by UIKit.
			/// This is associated with the <see cref="UIViewControllerSectionsTransitionInfo"/> class.
			/// </summary>
			UIViewControllerBased
		}

		public class DelegatingFrameSectionsTransitionInfo : FrameSectionsTransitionInfo
		{
			private readonly FrameSectionsTransitionDelegate _frameTranstion;

			/// <summary>
			/// Creates a new instance of <see cref="DelegatingFrameSectionsTransitionInfo"/>.
			/// </summary>
			/// <param name="frameTranstion">The method describing the transition.</param>
			public DelegatingFrameSectionsTransitionInfo(FrameSectionsTransitionDelegate frameTranstion)
			{
				_frameTranstion = frameTranstion;
			}

			///<inheritdoc/>
			public override FrameSectionsTransitionInfoTypes Type => FrameSectionsTransitionInfoTypes.FrameBased;

			/// <summary>
			/// Runs the transition.
			/// </summary>
			/// <param name="frameToHide">The <see cref="Frame"/> that must be hidden after the transition.</param>
			/// <param name="frameToShow">The <see cref="Frame"/> that must be visible after the transition.</param>
			/// <param name="frameToShowIsAboveFrameToHide">Flag indicating whether the frame to show is above the frame to hide in their parent container.</param>
			/// <returns>Task running the transition operation.</returns>
			public Task Run(Frame frameToHide, Frame frameToShow, bool frameToShowIsAboveFrameToHide)
			{
				return _frameTranstion(frameToHide, frameToShow, frameToShowIsAboveFrameToHide);
			}
		}

		public delegate Task FrameSectionsTransitionDelegate(Frame frameToHide, Frame frameToShow, bool frameToShowIsAboveFrameToHide);

		public class UIViewControllerSectionsTransitionInfo : FrameSectionsTransitionInfo
		{
#if __IOS__
			public UIViewControllerSectionsTransitionInfo(bool allowDismissFromGesture = true, UIModalPresentationStyle modalPresentationStyle = UIModalPresentationStyle.PageSheet, UIModalTransitionStyle modalTransitionStyle = UIModalTransitionStyle.CoverVertical)
			{
				AllowDismissFromGesture = allowDismissFromGesture;
				ModalPresentationStyle = modalPresentationStyle;
				ModalTransitionStyle = modalTransitionStyle;
			}

			public bool AllowDismissFromGesture { get; }

			public UIModalPresentationStyle ModalPresentationStyle { get; }

			public UIModalTransitionStyle ModalTransitionStyle { get; }
#endif

			public override FrameSectionsTransitionInfoTypes Type => FrameSectionsTransitionInfoTypes.UIViewControllerBased;
		}

		public static class Animations
		{
			/// <summary>
			/// The default duration of built-in animations, in seconds.
			/// </summary>
			public const double DefaultDuration = 0.250;

			/// <summary>
			/// Fades out <paramref name="frame1"/> to reveal <paramref name="frame2"/>.
			/// </summary>
			public static async Task FadeOutFrame1ToRevealFrame2(Frame frame1, Frame frame2)
			{
				// 1. Disable the currently visible frame during the animation.
				frame1.IsHitTestVisible = false;

				// 2. Make the next frame visible so that we see it as the previous frame fades out.
				frame2.Opacity = 1;
#if __IOS__ || __ANDROID__
				// TODO: Fix this workaround
				frame2.SetValue(UIElement.OpacityProperty, 1d, DependencyPropertyValuePrecedences.Animations);
#endif
				frame2.Visibility = Visibility.Visible;
				frame2.IsHitTestVisible = true;

				// 3. Fade out the frame.
				var storyboard = new Storyboard();
				AddFadeOut(storyboard, frame1);
				storyboard.Begin();
			}

			/// <summary>
			/// Fades in <paramref name="frame1"/> to hide <paramref name="frame2"/>.
			/// </summary>
			public static async Task FadeInFrame2ToHideFrame1(Frame frame1, Frame frame2)
			{
				// 1. Disable the currently visible frame during the animation.
				frame1.IsHitTestVisible = false;

				// 2. Make the next frame visible, but transparent.
				frame2.Opacity = 0;
#if __IOS__ || __ANDROID__
				// TODO: Fix this workaround
				frame2.SetValue(UIElement.OpacityProperty, 0d, DependencyPropertyValuePrecedences.Animations);
#endif
				frame2.Visibility = Visibility.Visible;

				// 3. Fade in the frame.
				var storyboard = new Storyboard();
				AddFadeIn(storyboard, frame2);
				storyboard.Begin();

				// 4. Once the next frame is visible, enable it.
				frame2.IsHitTestVisible = true;
			}

			/// <summary>
			/// Slides <paramref name="frame2"/> upwards to hide <paramref name="frame1"/>.
			/// </summary>
			public static async Task SlideFrame2UpwardsToHideFrame1(Frame frame1, Frame frame2)
			{
				frame1.IsHitTestVisible = false;
				((TranslateTransform)frame2.RenderTransform).Y = frame1.ActualHeight;
				frame2.Opacity = 1;
				frame2.Visibility = Visibility.Visible;

				var storyboard = new Storyboard();
				AddSlideInFromBottom(storyboard, (TranslateTransform)frame2.RenderTransform);
				storyboard.Begin();

				frame2.IsHitTestVisible = true;
			}

			/// <summary>
			/// Slides down <paramref name="frame1"/> to releave <paramref name="frame2"/>.
			/// </summary>
			public static async Task SlideFrame1DownToRevealFrame2(Frame frame1, Frame frame2)
			{
				frame1.IsHitTestVisible = false;
				frame2.Opacity = 1;
				frame2.Visibility = Visibility.Visible;

				var storyboard = new Storyboard();
				AddSlideBackToBottom(storyboard, (TranslateTransform)frame1.RenderTransform, frame2.ActualHeight);
				storyboard.Begin();

				frame2.IsHitTestVisible = true;
			}

			/// <summary>
			/// Collapses <paramref name="frame1"/> and make <paramref name="frame2"/> visible.
			/// </summary>
			public static Task CollapseFrame1AndShowFrame2(Frame frame1, Frame frame2)
			{
				frame1.Visibility = Visibility.Collapsed;
				frame2.IsHitTestVisible = false;

				frame2.Visibility = Visibility.Visible;
				frame2.Opacity = 1;
				frame2.IsHitTestVisible = true;

				return Task.CompletedTask;
			}

			private static void AddFadeIn(Storyboard storyboard, DependencyObject target)
			{
				var animation = new DoubleAnimation()
				{
					To = 1,
					Duration = new Duration(TimeSpan.FromSeconds(DefaultDuration)),
					EasingFunction = new QuadraticEase() { EasingMode = EasingMode.EaseInOut }
				};

				Storyboard.SetTarget(animation, target);
				Storyboard.SetTargetProperty(animation, "Opacity");

				storyboard.Children.Add(animation);
			}

			private static void AddFadeOut(Storyboard storyboard, DependencyObject target)
			{
				var animation = new DoubleAnimation()
				{
					To = 0,
					Duration = new Duration(TimeSpan.FromSeconds(DefaultDuration)),
					EasingFunction = new QuadraticEase() { EasingMode = EasingMode.EaseInOut }
				};

				Storyboard.SetTarget(animation, target);
				Storyboard.SetTargetProperty(animation, "Opacity");

				storyboard.Children.Add(animation);
			}

			private static void AddSlideInFromBottom(Storyboard storyboard, TranslateTransform target)
			{
				var animation = new DoubleAnimation()
				{
					To = 0,
					Duration = new Duration(TimeSpan.FromSeconds(DefaultDuration)),
					EasingFunction = new QuadraticEase() { EasingMode = EasingMode.EaseOut }
				};

				Storyboard.SetTarget(animation, target);
				Storyboard.SetTargetProperty(animation, "Y");

				storyboard.Children.Add(animation);
			}

			private static void AddSlideBackToBottom(Storyboard storyboard, TranslateTransform target, double translation)
			{
				var animation = new DoubleAnimation()
				{
					To = translation,
					Duration = new Duration(TimeSpan.FromSeconds(DefaultDuration)),
					EasingFunction = new QuadraticEase() { EasingMode = EasingMode.EaseOut }
				};

				Storyboard.SetTarget(animation, target);
				Storyboard.SetTargetProperty(animation, "Y");

				storyboard.Children.Add(animation);
			}
		}

		public abstract class SectionsTransitionInfo
		{
		}
	}

}
