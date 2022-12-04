﻿using Newtonsoft.Json;

namespace ED2.Core.Helpers;

public static class Json
{
    public static async Task<T> ToObjectAsync<T>(string value) => await Task.Run(() =>
                                                                       {
                                                                           return JsonConvert.DeserializeObject<T>(value);
                                                                       });

    public static async Task<string> StringifyAsync(object value) => await Task.Run(() =>
                                                                          {
                                                                              return JsonConvert.SerializeObject(value);
                                                                          });
}