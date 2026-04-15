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

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR

    // 키보드 훅
    [DllImport("user32.dll")] static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, uint dwThreadId);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] static extern bool UnhookWindowsHookEx(IntPtr hhk);
    [DllImport("user32.dll")] static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
    [DllImport("kernel32.dll")] static extern IntPtr GetModuleHandle(string lpModuleName);

    // 마우스 폴링 (훅 없음)
    [DllImport("user32.dll")] static extern short GetAsyncKeyState(int vKey);

    delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);

    const int WH_KEYBOARD_LL = 13;
    const int WM_KEYDOWN = 0x0100;
    const int WM_KEYUP = 0x0101;

    const int VK_LBUTTON = 0x01;
    const int VK_RBUTTON = 0x02;
    const int VK_MBUTTON = 0x04;

    private IntPtr keyboardHook = IntPtr.Zero;
    private HookProc keyboardProc;

    // 키보드 꾹 누르기 방지
    private ConcurrentDictionary<int, bool> heldKeys = new ConcurrentDictionary<int, bool>();

    // 마우스 이전 상태
    private bool wasLDown, wasRDown, wasMDown;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        keyboardProc = KeyboardHookCallback;
        InstallHooks();
    }

    void InstallHooks()
    {
        using (Process proc = Process.GetCurrentProcess())
        using (ProcessModule mod = proc.MainModule)
        {
            IntPtr hMod = GetModuleHandle(mod.ModuleName);
            keyboardHook = SetWindowsHookEx(WH_KEYBOARD_LL, keyboardProc, hMod, 0);
            // 마우스 훅 없음 — GetAsyncKeyState로 대체
        }

        if (keyboardHook == IntPtr.Zero)
            UnityEngine.Debug.LogWarning("[GlobalInputHook] 키보드 훅 설치 실패");
    }

    IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            int vkCode = Marshal.ReadInt32(lParam);
            int msg = (int)wParam;

            if (msg == WM_KEYDOWN)
            {
                if (heldKeys.TryAdd(vkCode, true))
                    System.Threading.Interlocked.Increment(ref pendingKeyPresses);
            }
            else if (msg == WM_KEYUP)
            {
                heldKeys.TryRemove(vkCode, out _);
            }
        }
        return CallNextHookEx(keyboardHook, nCode, wParam, lParam);
    }

    void Update()
    {
        // 키보드: 글로벌 훅에서 수신
        int keys = System.Threading.Interlocked.Exchange(ref pendingKeyPresses, 0);
        for (int i = 0; i < keys; i++)
            OnGlobalKeyPress?.Invoke();

        // 마우스: GetAsyncKeyState 폴링 (훅 없이 글로벌 감지, 성능 영향 0)
        bool lDown = (GetAsyncKeyState(VK_LBUTTON) & 0x8000) != 0;
        bool rDown = (GetAsyncKeyState(VK_RBUTTON) & 0x8000) != 0;
        bool mDown = (GetAsyncKeyState(VK_MBUTTON) & 0x8000) != 0;

        if (lDown && !wasLDown) OnGlobalMouseClick?.Invoke();
        if (rDown && !wasRDown) OnGlobalMouseClick?.Invoke();
        if (mDown && !wasMDown) OnGlobalMouseClick?.Invoke();

        wasLDown = lDown;
        wasRDown = rDown;
        wasMDown = mDown;
    }

    void OnApplicationQuit() => RemoveHooks();
    void OnDestroy() => RemoveHooks();

    void RemoveHooks()
    {
        if (keyboardHook != IntPtr.Zero) UnhookWindowsHookEx(keyboardHook);
        keyboardHook = IntPtr.Zero;
    }

#else
    // 에디터용
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