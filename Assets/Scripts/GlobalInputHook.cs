using UnityEngine;
using System;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Diagnostics;

public class GlobalInputHook : MonoBehaviour
{
    public static GlobalInputHook Instance { get; private set; }

    public event Action OnGlobalKeyPress;
    public event Action OnGlobalMouseClick;

    private int pendingKeyPresses;
    private int pendingMouseClicks;

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR

    [DllImport("user32.dll")] static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, uint dwThreadId);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] static extern bool UnhookWindowsHookEx(IntPtr hhk);
    [DllImport("user32.dll")] static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
    [DllImport("kernel32.dll")] static extern IntPtr GetModuleHandle(string lpModuleName);

    delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);

    const int WH_KEYBOARD_LL = 13;
    const int WH_MOUSE_LL = 14;
    const int WM_KEYDOWN = 0x0100;
    const int WM_KEYUP = 0x0101;
    const int WM_LBUTTONDOWN = 0x0201;
    const int WM_RBUTTONDOWN = 0x0204;
    const int WM_MBUTTONDOWN = 0x0207;

    private IntPtr keyboardHook = IntPtr.Zero;
    private IntPtr mouseHook = IntPtr.Zero;
    private HookProc keyboardProc;
    private HookProc mouseProc;

    // 꾹 누르기 방지: 현재 눌려있는 키 추적
    private ConcurrentDictionary<int, bool> heldKeys = new ConcurrentDictionary<int, bool>();

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        keyboardProc = KeyboardHookCallback;
        mouseProc = MouseHookCallback;
        InstallHooks();
    }

    void InstallHooks()
    {
        using (Process proc = Process.GetCurrentProcess())
        using (ProcessModule mod = proc.MainModule)
        {
            IntPtr hMod = GetModuleHandle(mod.ModuleName);
            keyboardHook = SetWindowsHookEx(WH_KEYBOARD_LL, keyboardProc, hMod, 0);
            mouseHook = SetWindowsHookEx(WH_MOUSE_LL, mouseProc, hMod, 0);
        }
    }

    IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            int vkCode = Marshal.ReadInt32(lParam);
            int msg = (int)wParam;

            if (msg == WM_KEYDOWN)
            {
                // TryAdd: 이미 있으면 false 반환 (꾹 누르기 반복 무시)
                // 새로 눌린 키만 카운트
                if (heldKeys.TryAdd(vkCode, true))
                {
                    System.Threading.Interlocked.Increment(ref pendingKeyPresses);
                }
            }
            else if (msg == WM_KEYUP)
            {
                // 키 뗐으면 추적에서 제거 (다음에 다시 누를 수 있게)
                heldKeys.TryRemove(vkCode, out _);
            }
        }
        return CallNextHookEx(keyboardHook, nCode, wParam, lParam);
    }

    IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            int msg = (int)wParam;
            if (msg == WM_LBUTTONDOWN || msg == WM_RBUTTONDOWN || msg == WM_MBUTTONDOWN)
            {
                System.Threading.Interlocked.Increment(ref pendingMouseClicks);
            }
        }
        return CallNextHookEx(mouseHook, nCode, wParam, lParam);
    }

    void Update()
    {
        int keys = System.Threading.Interlocked.Exchange(ref pendingKeyPresses, 0);
        for (int i = 0; i < keys; i++)
            OnGlobalKeyPress?.Invoke();

        int clicks = System.Threading.Interlocked.Exchange(ref pendingMouseClicks, 0);
        for (int i = 0; i < clicks; i++)
            OnGlobalMouseClick?.Invoke();
    }

    void OnApplicationQuit() => RemoveHooks();
    void OnDestroy() => RemoveHooks();

    void RemoveHooks()
    {
        if (keyboardHook != IntPtr.Zero) UnhookWindowsHookEx(keyboardHook);
        if (mouseHook != IntPtr.Zero) UnhookWindowsHookEx(mouseHook);
        keyboardHook = IntPtr.Zero;
        mouseHook = IntPtr.Zero;
    }

#else
    // 에디터용: Unity Input (anyKeyDown이 이미 첫 누름만 감지)
    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Update()
    {
        if (Input.anyKeyDown && !Input.GetMouseButtonDown(0) && !Input.GetMouseButtonDown(1) && !Input.GetMouseButtonDown(2))
            OnGlobalKeyPress?.Invoke();

        if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2))
            OnGlobalMouseClick?.Invoke();
    }
#endif
}